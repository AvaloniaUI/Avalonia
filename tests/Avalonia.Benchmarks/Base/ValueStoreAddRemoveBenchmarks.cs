using System;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;

namespace Avalonia.Benchmarks.Base;

internal class MockProperty : StyledProperty<int>
{
    public MockProperty([NotNull] string name) : base(name, typeof(object), new StyledPropertyMetadata<int>())
    {
    }
}

internal static class MockProperties
{
    public static readonly AvaloniaProperty[] LinearProperties;
    public static readonly AvaloniaProperty[] ShuffledProperties;

    static MockProperties()
    {
        LinearProperties = new AvaloniaProperty[32];
        ShuffledProperties = new AvaloniaProperty[32];

        for (int i = 0; i < LinearProperties.Length; i++)
        {
            LinearProperties[i] = ShuffledProperties[i] = new MockProperty($"Property#{i}");
        }
        
        Shuffle(ShuffledProperties, 42);
    }
    private static void Shuffle<T> (T[] array, int seed)
    {
        var rng = new Random(seed);
        
        int n = array.Length;
        while (n > 1) 
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}

[MemoryDiagnoser]
public class ValueStoreLookupBenchmarks
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    [Params(false, true)]
    public bool UseShuffledProperties;

    public AvaloniaProperty[] Properties => UseShuffledProperties ? MockProperties.ShuffledProperties : MockProperties.LinearProperties;

    private AvaloniaPropertyValueStore<object> _store;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new AvaloniaPropertyValueStore<object>();

        for (int i = 0; i < PropertyCount; i++)
        {
            _store.AddValue(Properties[i], null);
        }
    }

    [Benchmark]
    public void LookupProperties()
    {
        for (int i = 0; i < PropertyCount; i++)
        {
            _store.TryGetValue(Properties[i], out _);
        }
    }
}

[MemoryDiagnoser]
public class ValueStoreAddRemoveBenchmarks
{
    [Params(2, 6, 10, 20, 30)]
    public int PropertyCount;

    [Params(false, true)]
    public bool UseShuffledProperties;

    public AvaloniaProperty[] Properties => UseShuffledProperties ? MockProperties.ShuffledProperties : MockProperties.LinearProperties;
    
    [Benchmark]
    [Arguments(false)]
    [Arguments(true)]
    public void AddValue(bool isInitializing)
    {
        var store = new AvaloniaPropertyValueStore<object> { IsDuringInitialization = isInitializing };

        for (int i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
        }
    }
    
    [Benchmark]
    [Arguments(false)]
    [Arguments(true)]
    public void AddAndRemoveValue(bool isInitializing)
    {
        var store = new AvaloniaPropertyValueStore<object> { IsDuringInitialization = isInitializing };

        for (int i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
        }
        
        for (int i = PropertyCount - 1; i >= 0; i--)
        {
            store.Remove(Properties[i]);
        }
    }
    
    [Benchmark]
    [Arguments(false)]
    [Arguments(true)]
    public void AddAndRemoveValueInterleaved(bool isInitializing)
    {
        var store = new AvaloniaPropertyValueStore<object> { IsDuringInitialization = isInitializing };

        for (int i = 0; i < PropertyCount; i++)
        {
            store.AddValue(Properties[i], null);
            store.Remove(Properties[i]);
        }
    }
}
