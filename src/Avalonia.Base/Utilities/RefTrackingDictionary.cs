using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Utilities;

/// <summary>
/// Maintains a set of objects with reference counts
/// </summary>
internal class RefTrackingDictionary<TKey> : Dictionary<TKey, int> where TKey : class
{
    /// <summary>
    /// Increase reference count for a key by 1.
    /// </summary>
    /// <returns>true if key was added to the dictionary, false otherwise</returns>
    public bool AddRef(TKey key)
    {
#if NET5_0_OR_GREATER
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(this, key, out var _);
        count++;
#else
        TryGetValue(key, out var count);
        count++;
        this[key] = count;
#endif
        return count == 1;
    }

    /// <summary>
    /// Decrease reference count for a key by 1.
    /// </summary>
    /// <returns>true if key was removed to the dictionary, false otherwise</returns>
    public bool ReleaseRef(TKey key)
    {
#if NET5_0_OR_GREATER
        ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(this, key);
        if (Unsafe.IsNullRef(ref count))
#if DEBUG
            throw new InvalidOperationException("Attempting to release a non-referenced object");
#else
            return false;
#endif // DEBUG
        count--;
        if (count == 0)
        {
            Remove(key);
            return true;
        }

        return false;
#else
        if (!TryGetValue(key, out var count))
#if DEBUG
            throw new InvalidOperationException("Attempting to release a non-referenced object");
#else
            return false;
#endif // DEBUG
        count--;
        if (count == 0)
        {
            Remove(key);
            return true;
        }

        this[key] = count;
        return false;
#endif
    }
}