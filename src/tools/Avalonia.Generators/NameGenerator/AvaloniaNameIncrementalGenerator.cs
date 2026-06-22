using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XamlX;

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
                cancellationToken.ThrowIfCancellationRequested();
                var xaml = file.GetText(cancellationToken)?.ToString();
                if (xaml is null)
                {
                    return null;
                }

                ResolvedXmlView? resolvedXmlView;
                DiagnosticFactory? diagnosticFactory = null;
                var location =  new FileLinePositionSpan(file.Path, default);
                try
                {
                    var viewResolver = new XamlXViewResolver(s_noopCompiler);
                    var view = viewResolver.ResolveView(xaml, cancellationToken);
                    if (view is null)
                    {
                        return null;
                    }

                    var xmlNames = EquatableList<ResolvedXmlName>.Empty;
                    var nameResolver = new XamlXNameResolver();
                    xmlNames = nameResolver.ResolveXmlNames(view.Xaml, cancellationToken);

                    resolvedXmlView = new ResolvedXmlView(view, xmlNames);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (XmlException ex)
                {
                    diagnosticFactory = new(NameGeneratorDiagnostics.ParseFailed, new(file.Path, GetLinePositionSpan(ex)), new([ex.Message]));

                    resolvedXmlView = ex is XamlParseException ? TryExtractTypeFromXml(xaml) : null;
                }
                catch (XamlTypeSystemException ex)
                {
                    diagnosticFactory = new(NameGeneratorDiagnostics.ParseFailed, location, new([ex.Message]));
                    resolvedXmlView = TryExtractTypeFromXml(xaml);
                }
                catch (Exception ex)
                {
                    diagnosticFactory = GetInternalErrorDiagnostic(location, ex);
                    resolvedXmlView = null;
                }

                return new XmlClassInfo(file.Path, resolvedXmlView, diagnosticFactory);
            })
            .Where(request => request is not null)
            .WithTrackingName(TrackingNames.ParsedXamlClasses);

        // IMPORTANT: we shouldn't cache CompilationProvider as a whole,
        // But we also should keep in mind that CompilationProvider can frequently re-trigger generator.
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
            .Select(static (pair, ct) =>
            {
                var (classInfo, compiler) = pair;
                var hasDevToolsReference = compiler.TypeSystem.FindAssembly("Avalonia.Diagnostics") is not null;
                var nameResolver = new XamlXNameResolver();

                var diagnostics = new List<DiagnosticFactory>(2);
                if (classInfo?.Diagnostic != null)
                {
                    diagnostics.Add(classInfo.Diagnostic);
                }

                ResolvedView? view = null;
                if (classInfo?.XmlView is { } xmlView)
                {
                    var type = compiler.TypeSystem.FindType(xmlView.FullName);

                    if (type is null)
                    {
                        diagnostics.Add(new(NameGeneratorDiagnostics.InvalidType, new(classInfo.FilePath, default), new([xmlView.FullName])));
                    }
                    else if (type.IsAvaloniaStyledElement())
                    {
                        var resolvedNames = new List<ResolvedName>();
                        foreach (var xmlName in xmlView.XmlNames)
                        {
                            ct.ThrowIfCancellationRequested();

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
                            catch (XmlException ex)
                            {
                                diagnostics.Add(new(NameGeneratorDiagnostics.NamedElementFailed,
                                    new(classInfo.FilePath, GetLinePositionSpan(ex)), new([xmlName.Name, ex.Message])));
                            }
                            catch (Exception ex)
                            {
                                diagnostics.Add(GetInternalErrorDiagnostic(new(classInfo.FilePath, default), ex));
                            }
                        }

                        view = new ResolvedView(xmlView, type.IsAvaloniaWindow(), new(resolvedNames));
                    }
                }

                return new ResolvedClassInfo(view, hasDevToolsReference, new(diagnostics));
            })
            .WithTrackingName(TrackingNames.ResolvedNamesProvider);

        context.RegisterSourceOutput(resolvedNames.Combine(options), static (context, pair) =>
        {
            var (info, options) = pair;

            foreach (var diagnostic in info.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.Create());
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

    private static DiagnosticFactory GetInternalErrorDiagnostic(FileLinePositionSpan location, Exception ex) =>
        new(NameGeneratorDiagnostics.InternalError, location, new([ex.ToString().Replace('\n', '*').Replace('\r', '*')]));

    /// <summary>
    /// Fallback in case XAML parsing fails. Extracts just the class name and namespace of the root element.
    /// </summary>
    private static ResolvedXmlView? TryExtractTypeFromXml(string xaml)
    {
        try
        {
            var document = XDocument.Parse(xaml);
            var classValue = document.Root.Attribute(XName.Get("Class", XamlNamespaces.Xaml2006))?.Value;
            if (classValue?.LastIndexOf('.') is { } lastDotIndex && lastDotIndex != -1)
            {
                return new(classValue.Substring(lastDotIndex + 1), classValue.Substring(0, lastDotIndex), EquatableList<ResolvedXmlName>.Empty);
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static LinePositionSpan GetLinePositionSpan(XmlException ex)
    {
        var position = new LinePosition(Math.Max(0, ex.LineNumber - 1), Math.Max(0, ex.LinePosition - 1));
        return new(position, position);
    }

    internal record XmlClassInfo(
        string FilePath,
        ResolvedXmlView? XmlView,
        DiagnosticFactory? Diagnostic);

    internal record ResolvedClassInfo(
        ResolvedView? View,
        bool CanAttachDevTools,
        EquatableList<DiagnosticFactory> Diagnostics);

    /// <summary>
    /// Avoid holding references to <see cref="Diagnostic"/> because it can hold references to <see cref="ISymbol"/>, <see cref="SyntaxTree"/>, etc.
    /// </summary>
    internal record DiagnosticFactory(DiagnosticDescriptor Descriptor, FileLinePositionSpan LinePosition, EquatableList<string> FormatArguments)
    {
        public Diagnostic Create() => Diagnostic.Create(Descriptor, 
            Location.Create(LinePosition.Path, default, new(LinePosition.StartLinePosition, LinePosition.EndLinePosition)),
            messageArgs: [.. FormatArguments]);
    }
}
