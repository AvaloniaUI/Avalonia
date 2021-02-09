using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain
{
    internal interface IClassResolver
    {
        ResolvedClass ResolveClass(string xaml);
    }

    internal record ResolvedClass
    {
        public XamlDocument Xaml { get; }
        public string ClassName { get; }
        public string NameSpace { get; }

        public ResolvedClass(string className, string nameSpace, XamlDocument xaml)
        {
            ClassName = className;
            NameSpace = nameSpace;
            Xaml = xaml;
        }
    }
}