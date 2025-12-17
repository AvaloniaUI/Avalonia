using System;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Transport;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for BatchStream serialization performance.
    /// Tests the efficiency of compositor data transport.
    /// </summary>
    [MemoryDiagnoser]
    public class BatchStreamBenchmarks : IDisposable
    {
        private BatchStreamMemoryPool _memoryPool = null!;
        private BatchStreamObjectPool<object?> _objectPool = null!;
        private BatchStreamData _data = null!;
        private object[] _testObjects = null!;

        [Params(100, 500, 1000)]
        public int ItemCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _memoryPool = new BatchStreamMemoryPool(true);
            _objectPool = new BatchStreamObjectPool<object?>(true);
            _data = new BatchStreamData();
            
            _testObjects = new object[ItemCount];
            for (var i = 0; i < ItemCount; i++)
            {
                _testObjects[i] = new object();
            }
        }

        /// <summary>
        /// Measures the cost of writing primitive values to the batch stream.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WritePrimitives()
        {
            _data = new BatchStreamData();
            using (var writer = new BatchStreamWriter(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    writer.Write(i);
                    writer.Write((double)i);
                    writer.Write(true);
                }
            }
        }

        /// <summary>
        /// Measures the cost of writing Matrix values to the batch stream.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteMatrices()
        {
            _data = new BatchStreamData();
            using (var writer = new BatchStreamWriter(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    writer.Write(Matrix.Identity);
                }
            }
        }

        /// <summary>
        /// Measures the cost of writing object references to the batch stream.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteObjects()
        {
            _data = new BatchStreamData();
            using (var writer = new BatchStreamWriter(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    writer.WriteObject(_testObjects[i]);
                }
            }
        }

        /// <summary>
        /// Measures the cost of mixed writes (typical usage pattern).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteMixed()
        {
            _data = new BatchStreamData();
            using (var writer = new BatchStreamWriter(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    writer.WriteObject(_testObjects[i]);
                    writer.Write(Matrix.Identity);
                    writer.Write((double)i);
                    writer.Write(true);
                }
            }
        }

        /// <summary>
        /// Measures round-trip write+read performance.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteAndRead()
        {
            _data = new BatchStreamData();
            
            using (var writer = new BatchStreamWriter(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    writer.WriteObject(_testObjects[i]);
                    writer.Write(i);
                }
            }

            using (var reader = new BatchStreamReader(_data, _memoryPool, _objectPool))
            {
                for (var i = 0; i < ItemCount; i++)
                {
                    _ = reader.ReadObject();
                    _ = reader.Read<int>();
                }
            }
        }

        public void Dispose()
        {
            _memoryPool?.Dispose();
            _objectPool?.Dispose();
        }
    }
}
