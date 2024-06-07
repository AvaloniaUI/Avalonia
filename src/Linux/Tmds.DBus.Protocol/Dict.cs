using System.Collections;

namespace Tmds.DBus.Protocol;

// Using obsolete generic write members
#pragma warning disable CS0618

public sealed class Dict<TKey, TValue> : IDBusWritable, IDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _dict;

    public Dict() :
        this(new Dictionary<TKey, TValue>())
    { }

    public Dict(IDictionary<TKey, TValue> dictionary) :
        this(new Dictionary<TKey, TValue>(dictionary))
    { }

    private Dict(Dictionary<TKey, TValue> value)
    {
        TypeModel.EnsureSupportedVariantType<TKey>();
        TypeModel.EnsureSupportedVariantType<TValue>();
        _dict = value;
    }

    public Variant AsVariant() => Variant.FromDict(this);

    public static implicit operator Variant(Dict<TKey, TValue> value)
        => value.AsVariant();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteDictionary<TKey, TValue>(_dict);


    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dict.Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dict.Values;

    public int Count => _dict.Count;

    public TValue this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public void Add(TKey key, TValue value)
        => _dict.Add(key, value);

    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    public bool Remove(TKey key)
        => _dict.Remove(key);

    public bool TryGetValue(TKey key,
#if NET
                            [MaybeNullWhen(false)]
#endif
                            out TValue value)
        => _dict.TryGetValue(key, out value);

    public void Clear()
        => _dict.Clear();

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();
}
