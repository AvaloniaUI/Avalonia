namespace Avalonia.NameGenerator.Domain
{
    internal interface IClassResolver
    {
        ResolvedClass ResolveClass(string xaml);
    }

    internal record ResolvedClass
    {
        public string ClassName { get; }
        public string NameSpace { get; }

        public ResolvedClass(string className, string nameSpace)
        {
            ClassName = className;
            NameSpace = nameSpace;
        }
    }
}