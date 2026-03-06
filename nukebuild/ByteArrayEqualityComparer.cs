#nullable enable

using System;
using System.Collections.Generic;

public sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
    public static ByteArrayEqualityComparer Instance { get; } = new();

    public bool Equals(byte[]? x, byte[]? y) {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        return x.AsSpan().SequenceEqual(y.AsSpan());
    }

    public int GetHashCode(byte[]? obj)
    {
        var hashCode = new HashCode();
        hashCode.AddBytes(obj.AsSpan());
        return hashCode.ToHashCode();
    }
}
