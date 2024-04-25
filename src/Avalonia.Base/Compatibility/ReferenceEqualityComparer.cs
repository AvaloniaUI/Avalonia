#if NETSTANDARD2_0

using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    public new bool Equals(object? x, object? y)
        => ReferenceEquals(x, y);

    public int GetHashCode(object? obj)
        => RuntimeHelpers.GetHashCode(obj);
}

#endif
