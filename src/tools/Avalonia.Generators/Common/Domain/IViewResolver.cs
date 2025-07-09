using System.Collections.Immutable;
using XamlX.Ast;

namespace Avalonia.Generators.Common.Domain;

internal interface IViewResolver
{
    ResolvedViewDocument? ResolveView(string xaml);
}

internal record ResolvedViewInfo(string ClassName, string Namespace)
{
    public string FullName => $"{Namespace}.{ClassName}";
    public override string ToString() => FullName;
}

internal record ResolvedViewDocument(string ClassName, string Namespace, XamlDocument Xaml)
    : ResolvedViewInfo(ClassName, Namespace);

internal record ResolvedXmlView(
    string ClassName,
    string Namespace,
    ImmutableArray<ResolvedXmlName> XmlNames)
    : ResolvedViewInfo(ClassName, Namespace)
{
    public ResolvedXmlView(ResolvedViewInfo info, ImmutableArray<ResolvedXmlName> xmlNames)
        : this(info.ClassName, info.Namespace, xmlNames)
    {
        
    }
}

internal record ResolvedView(
    string ClassName,
    string Namespace,
    bool IsWindow,
    ImmutableArray<ResolvedName> Names)
    : ResolvedViewInfo(ClassName, Namespace)
{
    public ResolvedView(ResolvedViewInfo info, bool isWindow, ImmutableArray<ResolvedName> names)
        : this(info.ClassName, info.Namespace, isWindow, names)
    {
        
    }
}
