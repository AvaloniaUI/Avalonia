using System.Collections.Generic;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Compiler;

internal class NoopTypeSystem : IXamlTypeSystem
{
    public NoopTypeSystem()
        => WellKnownTypes = new XamlTypeWellKnownTypes(this);

    public IEnumerable<IXamlAssembly> Assemblies => [NoopAssembly.Instance];
    public XamlTypeWellKnownTypes WellKnownTypes { get; }
    public IXamlAssembly? FindAssembly(string substring) => null;
    public IXamlType? FindType(string name) => XamlPseudoType.Unresolved(name);
    public IXamlType? FindType(string name, string assembly) => XamlPseudoType.Unresolved(name);

    internal class NoopAssembly : IXamlAssembly
    {
        public static NoopAssembly Instance { get; } = new();
        public bool Equals(IXamlAssembly other) => ReferenceEquals(this, other);
        public string Name => "Noop";
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = [];
        public IXamlType? FindType(string fullName) => XamlPseudoType.Unresolved(fullName);
    }
}
