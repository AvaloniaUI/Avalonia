using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.NameGenerator;

internal class CompilationReferencesComparer : IEqualityComparer<Compilation>
{
    public bool Equals(Compilation x, Compilation y)
    {
        if (x.AssemblyName != y.AssemblyName)
        {
            return false;
        }

        if (x.ExternalReferences.Length != y.ExternalReferences.Length)
        {
            return false;
        }

        return x.ExternalReferences.OfType<PortableExecutableReference>().SequenceEqual(y.ExternalReferences.OfType<PortableExecutableReference>());
    }

    public int GetHashCode(Compilation obj)
    {
        return obj.References.GetHashCode();
    }
}
