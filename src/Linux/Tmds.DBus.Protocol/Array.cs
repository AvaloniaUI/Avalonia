using System.Collections;

namespace Tmds.DBus.Protocol;

// Using obsolete generic write members
#pragma warning disable CS0618

public sealed class Array<T> : IDBusWritable, IList<T>
    where T : notnull
{
    private readonly List<T> _values;

    public Array() :
        this(new List<T>())
    { }

    public Array(int capacity) :
        this(new List<T>(capacity))
    { }

    public Array(IEnumerable<T> collection) :
        this(new List<T>(collection))
    { }

    private Array(List<T> values)
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _values = values;
    }

    public void Add(T item)
        => _values.Add(item);

    public void Clear()
        => _values.Clear();

    public int Count => _values.Count;

    bool ICollection<T>.IsReadOnly
        => false;

    public T this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _values.GetEnumerator();

    public int IndexOf(T item)
        => _values.IndexOf(item);

    public void Insert(int index, T item)
        => _values.Insert(index, item);

    public void RemoveAt(int index)
        => _values.RemoveAt(index);

    public bool Contains(T item)
        => _values.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _values.CopyTo(array, arrayIndex);

    public bool Remove(T item)
        => _values.Remove(item);

    public Variant AsVariant()
        => Variant.FromArray(this);

    public static implicit operator Variant(Array<T> value)
        => value.AsVariant();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
    {
#if NET5_0_OR_GREATER
        Span<T> span = CollectionsMarshal.AsSpan(_values);
        writer.WriteArray<T>(span);
#else
        writer.WriteArray(_values);
#endif
    }
}
