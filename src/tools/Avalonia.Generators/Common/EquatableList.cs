using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Generators.Common;

// https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#pipeline-model-design
internal class EquatableList<T> : List<T>, IEquatable<EquatableList<T>>
{
    public EquatableList(IEnumerable<T> collection) : base(collection)
    {
        
    }

    public EquatableList()
    {
        
    }

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
        return this.Select(item => item?.GetHashCode() ?? 0).Aggregate(0, (x, y) => x ^ y);
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
