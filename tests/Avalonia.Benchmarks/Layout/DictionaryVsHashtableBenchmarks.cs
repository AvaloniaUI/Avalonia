using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks comparing Hashtable (boxing) vs Dictionary (no boxing) performance.
    /// This directly measures the performance impact of the Grid boxing issue.
    /// </summary>
    [MemoryDiagnoser]
    public class DictionaryVsHashtableBenchmarks
    {
        private readonly struct SpanKey : IEquatable<SpanKey>
        {
            public readonly int Start;
            public readonly int Count;
            public readonly bool U;

            public SpanKey(int start, int count, bool u)
            {
                Start = start;
                Count = count;
                U = u;
            }

            public bool Equals(SpanKey other) =>
                Start == other.Start && Count == other.Count && U == other.U;

            public override bool Equals(object? obj) =>
                obj is SpanKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(Start, Count, U);
        }

        private Hashtable _hashtable = null!;
        private Dictionary<SpanKey, double> _dictionary = null!;
        private SpanKey[] _keys = null!;

        [Params(100, 500, 1000)]
        public int KeyCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _hashtable = new Hashtable();
            _dictionary = new Dictionary<SpanKey, double>();
            _keys = new SpanKey[KeyCount];

            var random = new Random(42);
            for (var i = 0; i < KeyCount; i++)
            {
                _keys[i] = new SpanKey(random.Next(100), random.Next(10), random.Next(2) == 1);
            }
        }

        /// <summary>
        /// Hashtable write operations (current Grid implementation).
        /// Causes boxing of SpanKey and double.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Hashtable_Write()
        {
            _hashtable.Clear();
            for (var i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var value = (double)i;
                var existing = _hashtable[key];
                if (existing == null || value > (double)existing)
                {
                    _hashtable[key] = value; // Boxing both key and value
                }
            }
        }

        /// <summary>
        /// Dictionary write operations (proposed optimization).
        /// No boxing.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dictionary_Write()
        {
            _dictionary.Clear();
            for (var i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var value = (double)i;
                if (!_dictionary.TryGetValue(key, out var existing) || value > existing)
                {
                    _dictionary[key] = value; // No boxing
                }
            }
        }

        /// <summary>
        /// Hashtable read operations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double Hashtable_Read()
        {
            // First populate
            for (var i = 0; i < _keys.Length; i++)
            {
                _hashtable[_keys[i]] = (double)i;
            }

            // Then read
            double sum = 0;
            for (var i = 0; i < _keys.Length; i++)
            {
                var value = _hashtable[_keys[i]];
                if (value != null)
                    sum += (double)value;
            }

            _hashtable.Clear();
            return sum;
        }

        /// <summary>
        /// Dictionary read operations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double Dictionary_Read()
        {
            // First populate
            for (var i = 0; i < _keys.Length; i++)
            {
                _dictionary[_keys[i]] = (double)i;
            }

            // Then read
            double sum = 0;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_dictionary.TryGetValue(_keys[i], out var value))
                    sum += value;
            }

            _dictionary.Clear();
            return sum;
        }

        /// <summary>
        /// Full Grid-like cycle: clear, write, read, clear.
        /// Simulates a complete measure pass span storage pattern.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double Hashtable_FullCycle()
        {
            _hashtable.Clear();

            // Write phase (measure)
            for (var i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var value = (double)i;
                var existing = _hashtable[key];
                if (existing == null || value > (double)existing)
                {
                    _hashtable[key] = value;
                }
            }

            // Read phase (resolve)
            double sum = 0;
            for (var i = 0; i < _keys.Length; i++)
            {
                var value = _hashtable[_keys[i]];
                if (value != null)
                    sum += (double)value;
            }

            _hashtable.Clear();
            return sum;
        }

        /// <summary>
        /// Full cycle with Dictionary.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double Dictionary_FullCycle()
        {
            _dictionary.Clear();

            // Write phase (measure)
            for (var i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var value = (double)i;
                if (!_dictionary.TryGetValue(key, out var existing) || value > existing)
                {
                    _dictionary[key] = value;
                }
            }

            // Read phase (resolve)
            double sum = 0;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_dictionary.TryGetValue(_keys[i], out var value))
                    sum += value;
            }

            _dictionary.Clear();
            return sum;
        }
    }
}
