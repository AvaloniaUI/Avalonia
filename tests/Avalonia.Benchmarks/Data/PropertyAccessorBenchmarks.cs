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
        private readonly InpcPropertyAccessorPlugin _inpcPlugin = new InpcPropertyAccessorPlugin();
        private readonly MethodAccessorPlugin _methodPlugin = new MethodAccessorPlugin();
        private readonly TestObject _targetStrongRef = new TestObject();
        private readonly WeakReference<object> _targetWeakRef;

        public PropertyAccessorBenchmarks()
        {
            _targetWeakRef = new WeakReference<object>(_targetStrongRef);
        }

        [Benchmark]
        public void InpcAccessorMatch()
        {
            _inpcPlugin.Match(_targetWeakRef, nameof(TestObject.Test));
        }

        [Benchmark]
        public void InpcAccessorStart()
        {
            _inpcPlugin.Start(_targetWeakRef, nameof(TestObject.Test));
        }

        [Benchmark]
        public void MethodAccessorMatch()
        {
            _methodPlugin.Match(_targetWeakRef, nameof(TestObject.Execute));
        }

        [Benchmark]
        public void MethodAccessorStart()
        {
            _methodPlugin.Start(_targetWeakRef, nameof(TestObject.Execute));
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

            public void Execute()
            {
            }

            public void Execute(object p0)
            {
            }

            public void Execute(object p0, object p1)
            {
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
