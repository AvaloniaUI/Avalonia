using System;
using System.Collections.Specialized;
using Avalonia.Collections;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class CollectionBenchmarks
    {
        private AvaloniaList<int> _listWithCollectionChanged = null!;
        private AvaloniaList<int> _listWithoutCollectionChanged = null!;
        private AvaloniaDictionary<string, int> _dictionaryWithCollectionChanged = null!;
        private AvaloniaDictionary<string, int> _dictionaryWithoutCollectionChanged = null!;
        private int[] _itemsToAdd = null!;

        [Params(10, 100)]
        public int ItemCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _listWithCollectionChanged = new AvaloniaList<int>();
            _listWithCollectionChanged.CollectionChanged += OnCollectionChanged;

            _listWithoutCollectionChanged = new AvaloniaList<int>();

            _dictionaryWithCollectionChanged = new AvaloniaDictionary<string, int>();
            _dictionaryWithCollectionChanged.CollectionChanged += OnCollectionChanged;

            _dictionaryWithoutCollectionChanged = new AvaloniaDictionary<string, int>();

            _itemsToAdd = new int[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                _itemsToAdd[i] = i;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { }

        [Benchmark(Baseline = true)]
        public void AvaloniaList_AddRange_NoSubscribers()
        {
            _listWithoutCollectionChanged.AddRange(_itemsToAdd);
            _listWithoutCollectionChanged.Clear();
        }

        [Benchmark]
        public void AvaloniaList_AddRange_WithSubscribers()
        {
            _listWithCollectionChanged.AddRange(_itemsToAdd);
            _listWithCollectionChanged.Clear();
        }

        [Benchmark]
        public void AvaloniaList_Clear_NoSubscribers()
        {
            _listWithoutCollectionChanged.AddRange(_itemsToAdd);
            _listWithoutCollectionChanged.Clear();
        }

        [Benchmark]
        public void AvaloniaList_Clear_WithSubscribers()
        {
            // This path calls .ToArray() to create the event args
            _listWithCollectionChanged.AddRange(_itemsToAdd);
            _listWithCollectionChanged.Clear();
        }

        [Benchmark]
        public void AvaloniaDictionary_AddAndClear_NoSubscribers()
        {
            for (int i = 0; i < ItemCount; i++)
            {
                _dictionaryWithoutCollectionChanged[$"key{i}"] = i;
            }
            _dictionaryWithoutCollectionChanged.Clear();
        }

        [Benchmark]
        public void AvaloniaDictionary_AddAndClear_WithSubscribers()
        {
            // Clear path calls .ToArray() on dictionary
            for (int i = 0; i < ItemCount; i++)
            {
                _dictionaryWithCollectionChanged[$"key{i}"] = i;
            }
            _dictionaryWithCollectionChanged.Clear();
        }

        [Benchmark]
        public int AvaloniaList_Enumerate()
        {
            _listWithoutCollectionChanged.Clear();
            _listWithoutCollectionChanged.AddRange(_itemsToAdd);
            
            int sum = 0;
            foreach (var item in _listWithoutCollectionChanged)
            {
                sum += item;
            }
            return sum;
        }
    }
}
