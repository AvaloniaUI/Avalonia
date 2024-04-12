using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// An equality comparer for <see cref="Type"/>.
/// Use for performance when using a <see cref="Type"/> as a <see cref="Dictionary{TKey,TValue}"/>.
///
/// == is about twice as fast as Equals, because the latter also checks <see cref="Type.UnderlyingSystemType"/>
/// (see https://stackoverflow.com/questions/9234009/c-sharp-type-comparison-type-equals-vs-operator)
///
/// Inside a dictionary, this results in a ~15% speedup for a <see cref="Dictionary{TKey,TValue}.TryGetValue"/> call.
/// </summary>
internal sealed class TypeEqualityComparer : IEqualityComparer<Type>
{
    public static TypeEqualityComparer Instance { get; } = new();

    private TypeEqualityComparer()
    {
    }

    public bool Equals(Type? x, Type? y)
        => x == y;

    public int GetHashCode(Type obj)
        => RuntimeHelpers.GetHashCode(obj);
}
