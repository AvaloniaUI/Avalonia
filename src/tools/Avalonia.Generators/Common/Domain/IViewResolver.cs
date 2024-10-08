using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common.Domain;

internal interface IViewResolver
{
    ResolvedView? ResolveView(string xaml);
}

internal record ResolvedView(string ClassName, IXamlType XamlType, string Namespace, XamlDocument Xaml)
{
    public bool IsStyledElement { get; init; }
    public bool HasClass { get; init; }
}
