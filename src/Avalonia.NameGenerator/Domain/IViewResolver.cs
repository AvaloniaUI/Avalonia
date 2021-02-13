using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain
{
    internal interface IViewResolver
    {
        ResolvedView ResolveView(string xaml);
    }

    internal record ResolvedView
    {
        public XamlDocument Xaml { get; }
        public string ClassName { get; }
        public string Namespace { get; }

        public ResolvedView(string className, string nameSpace, XamlDocument xaml)
        {
            ClassName = className;
            Namespace = nameSpace;
            Xaml = xaml;
        }
    }
}