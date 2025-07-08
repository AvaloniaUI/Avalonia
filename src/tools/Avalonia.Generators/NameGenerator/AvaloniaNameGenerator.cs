using System;
using Avalonia.Generators.Common.Domain;
using Microsoft.CodeAnalysis.Text;

namespace Avalonia.Generators.NameGenerator;

internal class AvaloniaNameGenerator : INameGenerator
{
    private readonly ViewFileNamingStrategy _naming;
    private readonly IGlobPattern _namespacePattern;
    private readonly IViewResolver _classes;
    private readonly INameResolver _names;
    private readonly ICodeGenerator _code;

    public AvaloniaNameGenerator(
        ViewFileNamingStrategy naming,
        IGlobPattern namespacePattern,
        IViewResolver classes,
        INameResolver names,
        ICodeGenerator code)
    {
        _naming = naming;
        _namespacePattern = namespacePattern;
        _classes = classes;
        _names = names;
        _code = code;
    }

    public GeneratedPartialClass? GenerateNameReferences(SourceText sourceText)
    {
        var xaml = sourceText.ToString();
        var view = _classes.ResolveView(xaml);
        if (view is null
            || !_namespacePattern.Matches(view.Namespace))
        {
            return null;
        }

        var names = _names.ResolveNames(view.Xaml);
        var code = _code.GenerateCode(view.ClassName, view.Namespace, view.XamlType, names);
        var fileName = ResolveViewFileName(view, _naming);
        return new GeneratedPartialClass(fileName, code);
    }

    private static string ResolveViewFileName(ResolvedView view, ViewFileNamingStrategy strategy) => strategy switch
    {
        ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.cs",
        ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.cs",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown naming strategy!")
    };
}
