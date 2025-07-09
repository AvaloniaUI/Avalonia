using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using XamlX.Transform;

namespace Avalonia.Generators.NameGenerator;

[Generator(LanguageNames.CSharp)]
public class AvaloniaNameIncrementalGenerator : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";
    private static readonly MiniCompiler s_noopCompiler = MiniCompiler.CreateNoop();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Map MSBuild properties onto readonly GeneratorOptions.
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => new GeneratorOptions(options.GlobalOptions))
            .WithTrackingName(TrackingNames.XamlGeneratorOptionsProvider);

        // Filter additional texts, we only need Avalonia XAML files.
        var xamlFiles = context.AdditionalTextsProvider
            .Combine(options.Combine(context.AnalyzerConfigOptionsProvider))
            .Where(static pair =>
            {
                var text = pair.Left;
                var (options, optionsProvider) = pair.Right;
                var filePath = text.Path;

                if (!(filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                      filePath.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) ||
                      filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                if (!options.AvaloniaNameGeneratorFilterByPath.Matches(filePath))
                {
                    return false;
                }

                if (!optionsProvider.GetOptions(pair.Left).TryGetValue(SourceItemGroupMetadata, out var itemGroup)
                    || itemGroup != "AvaloniaXaml")
                {
                    return false;
                }

                return true;
            })
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.InputXamlFilesProvider);

        // Actual parsing step. We input XAML files one by one, but don't resolve any types.
        // That's why we use NoOp type system here, allowing parsing to run detached from C# compilation.
        // Otherwise we would need to re-parse XAML on any C# file changed.
        var parsedXamlClasses = xamlFiles
            .Select(static (file, cancellationToken) =>
            {
                var text = file.GetText(cancellationToken);
                var diagnostics = new List<DiagnosticDescriptor>();
                if (text is not null)
                {
                    try
                    {
                        var xaml = text.ToString();
                        var viewResolver = new XamlXViewResolver(s_noopCompiler);
                        var view = viewResolver.ResolveView(xaml);
                        if (view is null)
                        {
                            return null;
                        }

                        var nameResolver = new XamlXNameResolver();
                        var xmlNames = nameResolver.ResolveXmlNames(view.Xaml);

                        return new XmlClassInfo(
                            new ResolvedXmlView(view, xmlNames),
                            diagnostics.ToImmutableArray());
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add(GeneratorExtensions.NameGeneratorUnhandledError(ex));
                        return new XmlClassInfo(null, diagnostics.ToImmutableArray());
                    }
                }

                return null;
            })
            .Where(request => request is not null)
            .WithTrackingName(TrackingNames.ParsedXamlClasses);

        var compiler = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var roslynTypeSystem = new RoslynTypeSystem(compilation);
                return MiniCompiler.CreateRoslyn(roslynTypeSystem, MiniCompiler.AvaloniaXmlnsDefinitionAttribute);
            })
            .WithTrackingName(TrackingNames.XamlTypeSystem);

        // Note: this step will be re-executed on any C# file changes.
        // As much as possible heavy tasks should be moved outside of this step, like XAML parsing.
        var resolvedNames = parsedXamlClasses
            .Combine(compiler)
            .Select(static (pair, _) =>
            {
                var (classInfo, compiler) = pair;
                var hasDevToolsReference = compiler.TypeSystem.FindAssembly("Avalonia.Diagnostics") is not null;
                var nameResolver = new XamlXNameResolver();

                var diagnostics =  new List<DiagnosticDescriptor>(classInfo!.Diagnostics);
                ResolvedView? view = null;
                if (classInfo.XmlView is { } xmlView)
                {
                    var type = compiler.TypeSystem.FindType(xmlView.FullName);

                    if (type is null)
                    {
                        diagnostics.Add(GeneratorExtensions.NameGeneratorInvalidType(xmlView.FullName));
                    }
                    else if (type.IsAvaloniaStyledElement())
                    {
                        var resolvedNames = new List<ResolvedName>();
                        foreach (var xmlName in xmlView.XmlNames)
                        {
                            try
                            {
                                var clrType = compiler.ResolveXamlType(xmlName.XmlType);
                                if (!clrType.IsAvaloniaStyledElement())
                                {
                                    continue;
                                }

                                resolvedNames.Add(nameResolver
                                    .ResolveName(clrType, xmlName.Name, xmlName.FieldModifier));
                            }
                            catch (Exception ex)
                            {
                                diagnostics.Add(GeneratorExtensions.NameGeneratorUnhandledError(ex));
                            }
                        }

                        view = new ResolvedView(xmlView, type.IsAvaloniaWindow(), resolvedNames.ToImmutableArray());
                    }
                }

                return new ResolvedClassInfo(view, hasDevToolsReference, diagnostics.ToImmutableArray());
            })
            .WithTrackingName(TrackingNames.ResolvedNamesProvider);

        context.RegisterSourceOutput(resolvedNames.Combine(options), static (context, pair) =>
        {
            var (info, options) = pair;

            foreach (var diagnostic in info!.Diagnostics)
            {
                context.Report(diagnostic);
            }

            if (info.View is { } view && options.AvaloniaNameGeneratorFilterByNamespace.Matches(view.Namespace))
            {
                ICodeGenerator codeGenerator = options.AvaloniaNameGeneratorBehavior switch
                {
                    Behavior.OnlyProperties => new OnlyPropertiesCodeGenerator(
                        options.AvaloniaNameGeneratorClassFieldModifier),
                    Behavior.InitializeComponent => new InitializeComponentCodeGenerator(
                        options.AvaloniaNameGeneratorAttachDevTools && info.CanAttachDevTools && view.IsWindow,
                        options.AvaloniaNameGeneratorClassFieldModifier),
                    _ => throw new ArgumentOutOfRangeException()
                };
                var fileName = options.AvaloniaNameGeneratorViewFileNamingStrategy switch
                {
                    ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.cs",
                    ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.cs",
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(ViewFileNamingStrategy), options.AvaloniaNameGeneratorViewFileNamingStrategy,
                        "Unknown naming strategy!")
                };

                var generatedPartialClass = codeGenerator.GenerateCode(
                    info.View.ClassName,
                    info.View.Namespace,
                    info.View.Names);

                context.AddSource(fileName, generatedPartialClass);
            }
        });
    }

    internal record XmlClassInfo(
        ResolvedXmlView? XmlView,
        ImmutableArray<DiagnosticDescriptor> Diagnostics);

    internal record ResolvedClassInfo(
        ResolvedView? View,
        bool CanAttachDevTools,
        ImmutableArray<DiagnosticDescriptor> Diagnostics);
}
