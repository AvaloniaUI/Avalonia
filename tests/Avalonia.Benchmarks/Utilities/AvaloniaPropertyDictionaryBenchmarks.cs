using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Utilities;

// TODO: Remove after review together with related benchmark code.
internal sealed class AvaloniaPropertyValueStoreOld<TValue>
{
    // The last item in the list is always int.MaxValue.
    private static readonly Entry[] s_emptyEntries = { new Entry { PropertyId = int.MaxValue, Value = default! } };

    private Entry[] _entries;

    public AvaloniaPropertyValueStoreOld()
    {
        _entries = s_emptyEntries;
    }

    public int Count => _entries.Length - 1;
    public TValue this[int index] => _entries[index].Value;

    private (int, bool) TryFindEntry(int propertyId)
    {
        if (_entries.Length <= 12)
        {
            // For small lists, we use an optimized linear search. Since the last item in the list
            // is always int.MaxValue, we can skip a conditional branch in each iteration.
            // By unrolling the loop, we can skip another unconditional branch in each iteration.

            if (_entries[0].PropertyId >= propertyId)
                return (0, _entries[0].PropertyId == propertyId);
            if (_entries[1].PropertyId >= propertyId)
                return (1, _entries[1].PropertyId == propertyId);
            if (_entries[2].PropertyId >= propertyId)
                return (2, _entries[2].PropertyId == propertyId);
            if (_entries[3].PropertyId >= propertyId)
                return (3, _entries[3].PropertyId == propertyId);
            if (_entries[4].PropertyId >= propertyId)
                return (4, _entries[4].PropertyId == propertyId);
            if (_entries[5].PropertyId >= propertyId)
                return (5, _entries[5].PropertyId == propertyId);
            if (_entries[6].PropertyId >= propertyId)
                return (6, _entries[6].PropertyId == propertyId);
            if (_entries[7].PropertyId >= propertyId)
                return (7, _entries[7].PropertyId == propertyId);
            if (_entries[8].PropertyId >= propertyId)
                return (8, _entries[8].PropertyId == propertyId);
            if (_entries[9].PropertyId >= propertyId)
                return (9, _entries[9].PropertyId == propertyId);
            if (_entries[10].PropertyId >= propertyId)
                return (10, _entries[10].PropertyId == propertyId);
        }
        else
        {
            var low = 0;
            var high = _entries.Length;
            int id;

            while (high - low > 3)
            {
                var pivot = (high + low) / 2;
                id = _entries[pivot].PropertyId;

                if (propertyId == id)
                    return (pivot, true);

                if (propertyId <= id)
                    high = pivot;
                else
                    low = pivot + 1;
            }

            do
            {
                id = _entries[low].PropertyId;

                if (id == propertyId)
                    return (low, true);

                if (id > propertyId)
                    break;

                ++low;
            }
            while (low < high);
        }

        return (0, false);
    }

    public bool TryGetValue(AvaloniaProperty property, out TValue value)
    {
        (var index, var found) = TryFindEntry(property.Id);
        if (!found)
        {
            value = default;
            return false;
        }

        value = _entries[index].Value;
        return true;
    }

    public void AddValue(AvaloniaProperty property, TValue value)
    {
        var entries = new Entry[_entries.Length + 1];

        for (var i = 0; i < _entries.Length; ++i)
        {
            if (_entries[i].PropertyId > property.Id)
            {
                if (i > 0)
                {
                    Array.Copy(_entries, 0, entries, 0, i);
                }

                entries[i] = new Entry { PropertyId = property.Id, Value = value };
                Array.Copy(_entries, i, entries, i + 1, _entries.Length - i);
                break;
            }
        }

        _entries = entries;
    }

    public void SetValue(AvaloniaProperty property, TValue value)
    {
        _entries[TryFindEntry(property.Id).Item1].Value = value;
    }

    public void Remove(AvaloniaProperty property)
    {
        var (index, found) = TryFindEntry(property.Id);

        if (found)
        {
            var newLength = _entries.Length - 1;

            // Special case - one element left means that value store is empty so we can just reuse our "empty" array.
            if (newLength == 1)
            {
                _entries = s_emptyEntries;

                return;
            }

            var entries = new Entry[newLength];

            var ix = 0;

            for (var i = 0; i < _entries.Length; ++i)
            {
                if (i != index)
                {
                    entries[ix++] = _entries[i];
                }
            }

            _entries = entries;
        }
    }

    private struct Entry
    {
        internal int PropertyId;
        internal TValue Value;
    }
}

internal class MockProperty : StyledProperty<int>
{
    public MockProperty(string name) : base(name, typeof(object), typeof(object), new StyledPropertyMetadata<int>())
    {
    }
}

internal static class MockProperties
{
    public static readonly AvaloniaProperty[] ShuffledProperties;

    static MockProperties()
    {
        ShuffledProperties = new AvaloniaProperty[32];

        for (var i = 0; i < ShuffledProperties.Length; i++)
        {
            ShuffledProperties[i] = new MockProperty($"Property#{i}");
        }

        Shuffle(ShuffledProperties, 42);
    }

    private static void Shuffle<T>(T[] array, int seed)
    {
        var rng = new Random(seed);

        var n = array.Length;
        while (n > 1)
        {
            var k = rng.Next(n--);
            var temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}

[MemoryDiagnoser]
public class ValueStore_Lookup
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    public AvaloniaProperty[] Properties => MockProperties.ShuffledProperties;

    private AvaloniaPropertyDictionary<object> _store;
    private AvaloniaPropertyValueStoreOld<object> _oldStore;
    private Dictionary<AvaloniaProperty, object> _dictionary;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new AvaloniaPropertyDictionary<object>();
        _oldStore = new AvaloniaPropertyValueStoreOld<object>();
        _dictionary = new Dictionary<AvaloniaProperty, object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            _store.Add(Properties[i], null);
            _oldStore.AddValue(Properties[i], null);
            _dictionary.Add(Properties[i], null);
        }
    }

    [Benchmark]
    public void LookupProperties()
    {
        for (var i = 0; i < PropertyCount; i++)
        {
            _store.TryGetValue(Properties[i], out _);
        }
    }

    [Benchmark(Baseline = true)]
    public void LookupProperties_Old()
    {
        for (var i = 0; i < PropertyCount; i++)
        {
            _oldStore.TryGetValue(Properties[i], out _);
        }
    }

    [Benchmark]
    public void LookupProperties_Dict()
    {
        for (var i = 0; i < PropertyCount; i++)
        {
            _dictionary.TryGetValue(Properties[i], out _);
        }
    }
}

[MemoryDiagnoser]
public class ValueStore_AddBenchmarks
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    public AvaloniaProperty[] Properties => MockProperties.ShuffledProperties;

    [Benchmark]
    public void Add()
    {
        var store = new AvaloniaPropertyDictionary<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
        }
    }

    [Benchmark(Baseline = true)]
    public void Add_Old()
    {
        var store = new AvaloniaPropertyValueStoreOld<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
        }
    }

    [Benchmark]
    public void Add_Dict()
    {
        var store = new Dictionary<AvaloniaProperty, object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
        }
    }
}

[MemoryDiagnoser]
public class ValueStore_AddRemoveBenchmarks
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    public AvaloniaProperty[] Properties => MockProperties.ShuffledProperties;

    [Benchmark]
    public void AddAndRemoveValue()
    {
        var store = new AvaloniaPropertyDictionary<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
        }

        for (var i = PropertyCount - 1; i >= 0; i--)
        {
            store.Remove(Properties[i]);
        }
    }

    [Benchmark(Baseline = true)]
    public void AddAndRemoveValue_Old()
    {
        var store = new AvaloniaPropertyValueStoreOld<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
        }

        for (var i = PropertyCount - 1; i >= 0; i--)
        {
            store.Remove(Properties[i]);
        }
    }

    [Benchmark]
    public void AddAndRemoveValue_Dict()
    {
        var store = new Dictionary<AvaloniaProperty, object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
        }

        for (var i = PropertyCount - 1; i >= 0; i--)
        {
            store.Remove(Properties[i]);
        }
    }
}

[MemoryDiagnoser]
public class ValueStore_AddRemoveInterleavedBenchmarks
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    public AvaloniaProperty[] Properties => MockProperties.ShuffledProperties;

    [Benchmark]
    public void AddAndRemoveValueInterleaved()
    {
        var store = new AvaloniaPropertyDictionary<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
            store.Remove(Properties[i]);
        }
    }

    [Benchmark(Baseline = true)]
    public void AddAndRemoveValueInterleaved_Old()
    {
        var store = new AvaloniaPropertyValueStoreOld<object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
            store.Remove(Properties[i]);
        }
    }

    [Benchmark]
    public void AddAndRemoveValueInterleaved_Dict()
    {
        var store = new Dictionary<AvaloniaProperty, object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            store.Add(Properties[i], null);
            store.Remove(Properties[i]);
        }
    }
}


[MemoryDiagnoser]
public class ValueStore_Enumeration
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    public AvaloniaProperty[] Properties => MockProperties.ShuffledProperties;

    private AvaloniaPropertyDictionary<object> _store;
    private AvaloniaPropertyValueStoreOld<object> _oldStore;
    private Dictionary<AvaloniaProperty, object> _dictionary;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new AvaloniaPropertyDictionary<object>();
        _oldStore = new AvaloniaPropertyValueStoreOld<object>();
        _dictionary = new Dictionary<AvaloniaProperty, object>();

        for (var i = 0; i < PropertyCount; i++)
        {
            _store.Add(Properties[i], null);
            _oldStore.AddValue(Properties[i], null);
            _dictionary.Add(Properties[i], null);
        }
    }

    [Benchmark]
    public int Enumerate()
    {
        var result = 0;

        for (var i = 0; i < _store.Count; i++)
        {
            result += _store[i] is null ? 1 : 0;
        }

        return result;
    }

    [Benchmark(Baseline = true)]
    public int Enumerate_Old()
    {
        var result = 0;

        for (var i = 0; i < _store.Count; i++)
        {
            result += _oldStore[i] is null ? 1 : 0;
        }

        return result;
    }

    [Benchmark]
    public int Enumerate_Dict()
    {
        var result = 0;

        foreach (var i in _dictionary)
        {
            result += i.Value is null ? 1 : 0;
        }

        return result;
    }

    [Benchmark]
    public int Enumerate_DictValues()
    {
        var result = 0;

        foreach (var i in _dictionary.Values)
        {
            result += i is null ? 1 : 0;
        }

        return result;
    }
}
