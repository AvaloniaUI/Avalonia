using System.Collections.Generic;
using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain;

internal interface INameResolver
{
    IReadOnlyList<ResolvedName> ResolveNames(XamlDocument xaml);
}

internal record ResolvedName(string TypeName, string Name, string FieldModifier);