using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Data.Core.Plugins;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser, InProcess]
    public class PropertyAccessorBenchmarks
    {
        private readonly InpcPropertyAccessorPlugin _plugin = new InpcPropertyAccessorPlugin();
        private readonly TestObject _targetStrongRef = new TestObject();
        private readonly WeakReference<object> _targetWeakRef;

        public PropertyAccessorBenchmarks()
        {
            _targetWeakRef = new WeakReference<object>(_targetStrongRef);
        }

        [Benchmark]
        public void InpcAccessor()
        {
            _plugin.Start(_targetWeakRef, nameof(TestObject.Test));
        }

        private class TestObject : INotifyPropertyChanged
        {
            private string _test;

            public string Test
            {
                get => _test;
                set
                {
                    if (_test == value)
                    {
                        return;
                    }

                    _test = value;

                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
