using System.Collections.Immutable;
using XamlX.Ast;

namespace Avalonia.Generators.Common.Domain;

internal interface IViewResolver
{
    ResolvedView? ResolveView(string xaml);
}

internal record ResolvedViewInfo(string ClassName, string Namespace, bool IsWindow)
{
    public string FullName => $"{Namespace}.{ClassName}";
    public override string ToString() => FullName;
}

internal record ResolvedView(string ClassName, string Namespace, bool IsWindow, XamlDocument Xaml)
    : ResolvedViewInfo(ClassName, Namespace, IsWindow);

internal record ResolvedViewWithNames(
    string ClassName,
    string Namespace,
    bool IsWindow,
    ImmutableArray<ResolvedName> ResolvedNames)
    : ResolvedViewInfo(ClassName, Namespace, IsWindow)
{
    public ResolvedViewWithNames(ResolvedView view, ImmutableArray<ResolvedName> resolvedNames)
        : this(view.ClassName, view.Namespace, view.IsWindow, resolvedNames)
    {
        
    }
}
