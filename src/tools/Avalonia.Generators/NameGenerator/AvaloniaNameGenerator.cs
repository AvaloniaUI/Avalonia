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
    private readonly IViewResolver _classes;
    private readonly INameResolver _names;
    private readonly ICodeGenerator _code;

    public AvaloniaNameGenerator(
        ViewFileNamingStrategy naming,
        IGlobPattern pathPattern,
        IGlobPattern namespacePattern,
        IViewResolver classes,
        INameResolver names,
        ICodeGenerator code)
    {
        _naming = naming;
        _pathPattern = pathPattern;
        _namespacePattern = namespacePattern;
        _classes = classes;
        _names = names;
        _code = code;
    }

    public IEnumerable<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles, CancellationToken cancellationToken)
    {
        var resolveViews =
            from file in additionalFiles
            let filePath = file.Path
            where (filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)) &&
                  _pathPattern.Matches(filePath)
            let xaml = file.GetText(cancellationToken)?.ToString()
            where xaml != null
            let view = _classes.ResolveView(xaml)
            where view != null && _namespacePattern.Matches(view.Namespace)
            select view;

        var query =
            from view in resolveViews
            let names = _names.ResolveNames(view.Xaml)
            let code = _code.GenerateCode(view.ClassName, view.Namespace, view.XamlType, names)
            let fileName = ResolveViewFileName(view, _naming)
            select new GeneratedPartialClass(fileName, code);

        return query;
    }

    private static string ResolveViewFileName(ResolvedView view, ViewFileNamingStrategy strategy) => strategy switch
    {
        ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.cs",
        ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.cs",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown naming strategy!")
    };
}
