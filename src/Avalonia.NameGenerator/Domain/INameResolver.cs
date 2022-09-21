using System.Collections.Generic;
using System.Linq;

using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain;

internal interface INameResolver
{
    IReadOnlyList<ResolvedName> ResolveNames(XamlDocument xaml);
}

internal class ResolvedName
{
    public ResolvedName(string typeName, string name, string fieldModifier, IReadOnlyList<string> genericTypeArguments)
    {
        TypeName = typeName;
        Name = name;
        FieldModifier = fieldModifier;
        GenericTypeArguments = genericTypeArguments;
    }

    public string TypeName { get; }
    public string Name { get; }
    public string FieldModifier { get; }
    public IReadOnlyList<string> GenericTypeArguments { get; }

    public string PrintableTypeName =>
        GenericTypeArguments.Count == 0
            ? $"global::{TypeName}"
            : $@"global::{TypeName}<{string.Join(", ", GenericTypeArguments.Select(arg => $"global::{arg}"))}>";

    public void Deconstruct(out string typeName, out string name, out string fieldModifier)
    {
        typeName = TypeName;
        name = Name;
        fieldModifier = FieldModifier;
    }

    public override bool Equals(object obj)
    {
        if (obj is not ResolvedName name)
        {
            return false;
        }

        return name.TypeName == TypeName
            && name.Name == Name
            && name.FieldModifier == FieldModifier
            && name.GenericTypeArguments.SequenceEqual(GenericTypeArguments);
    }

    public override int GetHashCode()
    {
        return (TypeName, Name, FieldModifier).GetHashCode();
    }
}