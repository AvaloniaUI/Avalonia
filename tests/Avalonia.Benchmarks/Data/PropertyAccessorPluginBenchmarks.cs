using System.Collections.Generic;
using Avalonia.Data.Core.Plugins;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser, InProcess]
    public class PropertyAccessorPluginBenchmarks
    {
        private readonly AccessorTestObject _targetStrongRef = new AccessorTestObject();

        private readonly List<IPropertyAccessorPlugin> _oldPlugins;
        private readonly List<IPropertyAccessorPlugin> _newPlugins;

        public PropertyAccessorPluginBenchmarks()
        {
            _oldPlugins = new List<IPropertyAccessorPlugin>
            {
                new AvaloniaPropertyAccessorPlugin(),
                new ReflectionMethodAccessorPlugin(),
                new InpcPropertyAccessorPlugin()
            };

            _newPlugins = new List<IPropertyAccessorPlugin>
            {
                new AvaloniaPropertyAccessorPlugin(),
                new InpcPropertyAccessorPlugin(),
                new ReflectionMethodAccessorPlugin()
            };
        }

        [Benchmark]
        public void MatchAccessorOld()
        {
            var propertyName = nameof(AccessorTestObject.Test);

            foreach (IPropertyAccessorPlugin x in _oldPlugins)
            {
                if (x.Match(_targetStrongRef, propertyName))
                {
                    break;
                }
            }
        }

        [Benchmark]
        public void MatchAccessorNew()
        {
            var propertyName = nameof(AccessorTestObject.Test);

            foreach (IPropertyAccessorPlugin x in _newPlugins)
            {
                if (x.Match(_targetStrongRef, propertyName))
                {
                    break;
                }
            }
        }
    }
}
