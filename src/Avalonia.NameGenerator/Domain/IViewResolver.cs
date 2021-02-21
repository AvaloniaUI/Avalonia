using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain
{
    internal interface IViewResolver
    {
        ResolvedView ResolveView(string xaml);
    }

    internal record ResolvedView(string ClassName, string Namespace, XamlDocument Xaml);
}