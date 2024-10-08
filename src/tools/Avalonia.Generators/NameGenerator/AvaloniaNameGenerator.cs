using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Generators.Common.Domain;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.NameGenerator;

internal class AvaloniaNameGenerator : INameGenerator
{
    private readonly ViewFileNamingStrategy _naming;
    private readonly IGlobPattern _pathPattern;
    private readonly IGlobPattern _namespacePattern;
    private readonly IViewResolver _views;
    private readonly INameResolver _names;
    private readonly IClassResolver _classes;
    private readonly ICodeGenerator _code;

    public AvaloniaNameGenerator(
        ViewFileNamingStrategy naming,
        IGlobPattern pathPattern,
        IGlobPattern namespacePattern,
        IViewResolver views,
        INameResolver names,
        IClassResolver classes,
        ICodeGenerator code)
    {
        _naming = naming;
        _pathPattern = pathPattern;
        _namespacePattern = namespacePattern;
        _views = views;
        _names = names;
        _classes = classes;
        _code = code;
    }

    public IEnumerable<GeneratedPartialClass> GenerateSupportClasses(IEnumerable<AdditionalText> additionalFiles, CancellationToken cancellationToken)
    {
        var resolveViews = additionalFiles.Select(file => (file, filePath: file.Path))
            .Where(t =>
                (t.filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                 t.filePath.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) ||
                 t.filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)) &&
                _pathPattern.Matches(t.filePath))
            .Select(t => t.file.GetText(cancellationToken)?.ToString())
            .Where(xaml => xaml != null)
            .Select(xaml => _views.ResolveView(xaml!))
            .Where(view => view != null && _namespacePattern.Matches(view.Namespace))!
            .ToArray<ResolvedView>();

        var xNameFiles = resolveViews
            .Select(view => (view, names: _names.ResolveNames(view.Xaml)))
            .Where(t => !t.view.HasClass || (t.view.IsStyledElement && t.names.Any()))
            .Select(t => (
                fileName: ResolveViewFileName(t.view, _naming),
                code: _code.GenerateCode(t.view, t.names)))
            .Select(t => new GeneratedPartialClass(t.fileName, t.code));

        return xNameFiles;
    }

    private static string ResolveViewFileName(ResolvedView view, ViewFileNamingStrategy strategy) => strategy switch
    {
        ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.cs",
        ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.cs",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown naming strategy!")
    };
}
