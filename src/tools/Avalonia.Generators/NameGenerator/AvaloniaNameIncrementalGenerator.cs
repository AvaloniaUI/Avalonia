using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.NameGenerator;

[Generator(LanguageNames.CSharp)]
public class AvaloniaNameIncrementalGenerator : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

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

        // Map compilation into readonly XAML type system.
        // Which is ONLY updated when any compilation references have changed.
        var typeSystem = context.CompilationProvider
            .WithComparer(new CompilationReferencesComparer())
            .Select(static (compilation, _) => new RoslynTypeSystem((CSharpCompilation)compilation))
            .WithTrackingName(TrackingNames.XamlTypeSystemProvider);

        // Actual parsing step. We input XAML files one by one, and reuse readonly type system.
        // It's detached from the up-to-date compilation info, we can't access type information here yet.
        // Otherwise slow parsing would slow down IDE on any file edited.
        // This pipeline step only depends on the input xaml tiles and type system (which depends on assembly references, but not actual types).
        var partialFilesInfo = xamlFiles.Combine(typeSystem)
            .Select(static (pair, cancellationToken) =>
            {
                var (file, types) = pair;

                var compiler = MiniCompiler.CreateDefault(types, MiniCompiler.AvaloniaXmlnsDefinitionAttribute);
                var canAttachDevTools = types.FindAssembly("Avalonia.Diagnostics") is not null;
                var text = file.GetText(cancellationToken);
                var diagnostics = new List<DiagnosticDescriptor>();
                if (text is not null)
                {
                    try
                    {
                        var xaml = text.ToString();
                        var viewResolver = new XamlXViewResolver(
                            types, compiler, true,
                            invalidType =>
                                diagnostics.Add(GeneratorExtensions.NameGeneratorInvalidType(invalidType)));
                        var view = viewResolver.ResolveView(xaml);
                        if (view is null)
                        {
                            return null;
                        }

                        var nameResolver = new XamlXNameResolver();
                        var resolvedNames = nameResolver.ResolveNames(view.Xaml);

                        return new PartialClassInfo(
                            new ResolvedViewWithNames(view, resolvedNames),
                            canAttachDevTools,
                            diagnostics.ToImmutableArray());
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add(GeneratorExtensions.NameGeneratorUnhandledError(ex));
                        return new PartialClassInfo(null, canAttachDevTools, diagnostics.ToImmutableArray());
                    }
                }

                return null;
            })
            .Where(request => request is not null)
            .WithTrackingName(TrackingNames.ParsedXamlPartialFiles);

        context.RegisterSourceOutput(partialFilesInfo.Combine(options), static (context, pair) =>
        {
            var (info, options) = pair;

            foreach (var diagnostic in info!.Diagnostics)
            {
                context.Report(diagnostic);
            }

            if (info.ViewInfo is { } view && options.AvaloniaNameGeneratorFilterByNamespace.Matches(view.Namespace))
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
                    info.ViewInfo.ClassName,
                    info.ViewInfo.Namespace,
                    info.ViewInfo.ResolvedNames);

                context.AddSource(fileName, generatedPartialClass);
            }
        });
    }
}

internal record PartialClassInfo(
    ResolvedViewWithNames? ViewInfo,
    bool CanAttachDevTools,
    ImmutableArray<DiagnosticDescriptor> Diagnostics);
