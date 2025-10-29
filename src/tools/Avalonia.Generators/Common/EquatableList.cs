using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.Generators.Common;

// https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#pipeline-model-design
// With minor modification to use ReadOnlyCollection instead of List
internal class EquatableList<T>(IList<T> collection)
    : ReadOnlyCollection<T>(collection), IEquatable<EquatableList<T>>
{
    public static readonly EquatableList<T> Empty = new([]);

    public bool Equals(EquatableList<T>? other)
    {
        // If the other list is null or a different size, they're not equal
        if (other is null || Count != other.Count)
        {
            return false;
        }

        // Compare each pair of elements for equality
        for (int i = 0; i < Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(this[i], other[i]))
            {
                return false;
            }
        }

        // If we got this far, the lists are equal
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as EquatableList<T>);
    }

    public override int GetHashCode()
    {
        var hash = 0;
        for (var i = 0; i < Count; i++)
        {
            hash ^= this[i]?.GetHashCode() ?? 0;
        }
        return hash;
    }

    public static bool operator ==(EquatableList<T>? list1, EquatableList<T>? list2)
    {
        return ReferenceEquals(list1, list2)
               || list1 is not null && list2 is not null && list1.Equals(list2);
    }

    public static bool operator !=(EquatableList<T>? list1, EquatableList<T>? list2)
    {
        return !(list1 == list2);
    }
}
