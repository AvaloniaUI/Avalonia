using System.Collections.Immutable;
using System.Threading;
using XamlX.Ast;

namespace Avalonia.Generators.Common.Domain;

internal interface IViewResolver
{
    ResolvedViewDocument? ResolveView(string xaml, CancellationToken cancellationToken);
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
    EquatableList<ResolvedXmlName> XmlNames)
    : ResolvedViewInfo(ClassName, Namespace)
{
    public ResolvedXmlView(ResolvedViewInfo info, EquatableList<ResolvedXmlName> xmlNames)
        : this(info.ClassName, info.Namespace, xmlNames)
    {
        
    }
}

internal record ResolvedView(
    string ClassName,
    string Namespace,
    bool IsWindow,
    EquatableList<ResolvedName> Names)
    : ResolvedViewInfo(ClassName, Namespace)
{
    public ResolvedView(ResolvedViewInfo info, bool isWindow, EquatableList<ResolvedName> names)
        : this(info.ClassName, info.Namespace, isWindow, names)
    {
        
    }
}
