using System;
using Avalonia.Data.Core.Plugins;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser, InProcess]
    public class PropertyAccessorBenchmarks
    {
        private readonly InpcPropertyAccessorPlugin _inpcPlugin = new InpcPropertyAccessorPlugin();
        private readonly ReflectionMethodAccessorPlugin _methodPlugin = new ReflectionMethodAccessorPlugin();
        private readonly AccessorTestObject _targetStrongRef = new AccessorTestObject();
        private readonly WeakReference<object> _targetWeakRef;

        public PropertyAccessorBenchmarks()
        {
            _targetWeakRef = new WeakReference<object>(_targetStrongRef);
        }

        [Benchmark]
        public void InpcAccessorMatch()
        {
            _inpcPlugin.Match(_targetWeakRef, nameof(AccessorTestObject.Test));
        }

        [Benchmark]
        public void InpcAccessorStart()
        {
            _inpcPlugin.Start(_targetWeakRef, nameof(AccessorTestObject.Test));
        }

        [Benchmark]
        public void MethodAccessorMatch()
        {
            _methodPlugin.Match(_targetWeakRef, nameof(AccessorTestObject.Execute));
        }

        [Benchmark]
        public void MethodAccessorStart()
        {
            _methodPlugin.Start(_targetWeakRef, nameof(AccessorTestObject.Execute));
        }
    }
}
