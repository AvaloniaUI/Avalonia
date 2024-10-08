using XamlX.Ast;

namespace Avalonia.Generators.Common.Domain;

internal interface IClassResolver
{
    ResolvedClass? ResolveClass(XamlDocument xaml);
}

internal record ResolvedClass(string TypeName, string ClassModifier);
