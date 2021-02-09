using System.Collections.Generic;

namespace Avalonia.NameGenerator.Domain
{
    internal interface INameResolver
    {
        IReadOnlyList<ResolvedName> ResolveNames(string xaml);
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