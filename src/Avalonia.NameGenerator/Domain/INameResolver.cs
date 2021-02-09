using System.Collections.Generic;
using XamlX.Ast;

namespace Avalonia.NameGenerator.Domain
{
    internal interface INameResolver
    {
        IReadOnlyList<ResolvedName> ResolveNames(XamlDocument xaml);
    }

    internal record ResolvedName
    {
        public string TypeName { get; }
        public string Name { get; }
        public string FieldModifier { get; }

        public ResolvedName(string typeName, string name, string fieldModifier)
        {
            TypeName = typeName;
            Name = name;
            FieldModifier = fieldModifier;
        }
    }
}