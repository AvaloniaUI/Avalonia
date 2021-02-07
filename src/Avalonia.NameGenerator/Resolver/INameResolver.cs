using System.Collections.Generic;

namespace Avalonia.NameGenerator.Resolver
{
    internal interface INameResolver
    {
        IReadOnlyList<ResolvedName> ResolveNames(string xaml);
    }

    internal class ResolvedName
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

        public override bool Equals(object obj)
        {
            if (obj is not ResolvedName name)
                return false;
            return name.Name == Name &&
                   name.TypeName == TypeName &&
                   name.FieldModifier == FieldModifier;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TypeName != null ? TypeName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FieldModifier != null ? FieldModifier.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}