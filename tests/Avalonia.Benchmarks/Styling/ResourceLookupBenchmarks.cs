using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ResourceLookupBenchmarks
    {
        private TestRoot _root = null!;
        private Control _deepChild = null!;
        private IDisposable _app = null!;
        private const string ExistingKey = "TestBrush";
        private const string NonExistingKey = "NonExistingKey";
        private const string DeepKey = "DeepResource";

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot();
            _root.Resources[ExistingKey] = Avalonia.Media.Brushes.Red;

            // Create nested structure with resources at different levels
            var current = new Border();
            _root.Child = current;

            for (int i = 0; i < 10; i++)
            {
                var child = new Border();
                current.Child = child;
                // Add some resources at each level
                child.Resources[$"Level{i}Resource"] = i;
                current = child;
            }

            // Add a deep resource
            current.Resources[DeepKey] = "DeepValue";
            _deepChild = current;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _app?.Dispose();
        }

        [Benchmark(Baseline = true)]
        public object? TryFindResource_Exists_AtRoot()
        {
            _deepChild.TryFindResource(ExistingKey, out var value);
            return value;
        }

        [Benchmark]
        public object? TryFindResource_Exists_AtDeep()
        {
            _deepChild.TryFindResource(DeepKey, out var value);
            return value;
        }

        [Benchmark]
        public object? TryFindResource_NotExists()
        {
            _deepChild.TryFindResource(NonExistingKey, out var value);
            return value;
        }

        [Benchmark]
        public object? FindResource_Exists()
        {
            return _deepChild.FindResource(ExistingKey);
        }

        [Benchmark]
        public object? ResourceDictionary_TryGetValue()
        {
            _root.Resources.TryGetValue(ExistingKey, out var value);
            return value;
        }

        [Benchmark]
        public object? ResourceDictionary_Indexer()
        {
            return _root.Resources[ExistingKey];
        }

        [Benchmark]
        public bool ResourceDictionary_ContainsKey()
        {
            return _root.Resources.ContainsKey(ExistingKey);
        }

        [Benchmark]
        public void ResourceDictionary_AddRemove()
        {
            _root.Resources["TempKey"] = 42;
            _root.Resources.Remove("TempKey");
        }
    }
}
