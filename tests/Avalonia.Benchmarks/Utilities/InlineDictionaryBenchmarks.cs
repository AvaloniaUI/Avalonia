using System;
using System.Collections.Generic;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Utilities
{
    [MemoryDiagnoser]
    public class InlineDictionaryBenchmarks
    {
        private readonly object _key1 = new object();
        private readonly object _key2 = new object();
        private readonly object _key3 = new object();
        private readonly object _key4 = new object();
        private readonly object _key5 = new object();

        /// <summary>
        /// Benchmark InlineDictionary with single item (optimized path)
        /// </summary>
        [Benchmark(Baseline = true)]
        public int InlineDictionary_SingleItem()
        {
            var dict = new InlineDictionary<object, int>();
            dict.Set(_key1, 42);
            return dict.TryGetValue(_key1, out var value) ? value : 0;
        }

        /// <summary>
        /// Benchmark regular Dictionary with single item
        /// </summary>
        [Benchmark]
        public int RegularDictionary_SingleItem()
        {
            var dict = new Dictionary<object, int>();
            dict[_key1] = 42;
            return dict.TryGetValue(_key1, out var value) ? value : 0;
        }

        /// <summary>
        /// Benchmark InlineDictionary with two items
        /// </summary>
        [Benchmark]
        public int InlineDictionary_TwoItems()
        {
            var dict = new InlineDictionary<object, int>();
            dict.Set(_key1, 1);
            dict.Set(_key2, 2);
            return (dict.TryGetValue(_key1, out var v1) ? v1 : 0) +
                   (dict.TryGetValue(_key2, out var v2) ? v2 : 0);
        }

        /// <summary>
        /// Benchmark regular Dictionary with two items
        /// </summary>
        [Benchmark]
        public int RegularDictionary_TwoItems()
        {
            var dict = new Dictionary<object, int>();
            dict[_key1] = 1;
            dict[_key2] = 2;
            return (dict.TryGetValue(_key1, out var v1) ? v1 : 0) +
                   (dict.TryGetValue(_key2, out var v2) ? v2 : 0);
        }

        /// <summary>
        /// Benchmark InlineDictionary with five items (threshold before dictionary upgrade)
        /// </summary>
        [Benchmark]
        public int InlineDictionary_FiveItems()
        {
            var dict = new InlineDictionary<object, int>();
            dict.Set(_key1, 1);
            dict.Set(_key2, 2);
            dict.Set(_key3, 3);
            dict.Set(_key4, 4);
            dict.Set(_key5, 5);
            return (dict.TryGetValue(_key1, out var v1) ? v1 : 0) +
                   (dict.TryGetValue(_key3, out var v3) ? v3 : 0) +
                   (dict.TryGetValue(_key5, out var v5) ? v5 : 0);
        }

        /// <summary>
        /// Benchmark regular Dictionary with five items
        /// </summary>
        [Benchmark]
        public int RegularDictionary_FiveItems()
        {
            var dict = new Dictionary<object, int>();
            dict[_key1] = 1;
            dict[_key2] = 2;
            dict[_key3] = 3;
            dict[_key4] = 4;
            dict[_key5] = 5;
            return (dict.TryGetValue(_key1, out var v1) ? v1 : 0) +
                   (dict.TryGetValue(_key3, out var v3) ? v3 : 0) +
                   (dict.TryGetValue(_key5, out var v5) ? v5 : 0);
        }
    }
}
