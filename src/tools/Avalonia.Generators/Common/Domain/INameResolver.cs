using System.Collections.Generic;
using XamlX.Ast;

namespace Avalonia.Generators.Common.Domain;

internal enum NamedFieldModifier
{
    Public = 0,
    Private = 1,
    Internal = 2,
    Protected = 3,
}

internal interface INameResolver
{
    IReadOnlyList<ResolvedName> ResolveNames(XamlDocument xaml);
}

internal record ResolvedName(string TypeName, string Name, string FieldModifier);
