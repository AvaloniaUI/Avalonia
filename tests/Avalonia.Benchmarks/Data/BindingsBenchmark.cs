﻿using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser, InProcess]
    public class BindingsBenchmark
    {
        [Benchmark]
        public void TwoWayBinding_Via_Binding()
        {
            var instance = new TestClass();

            var binding = new Binding(nameof(TestClass.BoundValue), BindingMode.TwoWay)
            {
                Source = instance
            };

            instance.Bind(TestClass.IntValueProperty, binding);
        }

        [Benchmark]
        public void OneWayBinding_Via_Binding()
        {
            var instance = new TestClass();

            var binding = new Binding(nameof(TestClass.BoundValue), BindingMode.OneWay)
            {
                Source = instance
            };

            instance.Bind(TestClass.IntValueProperty, binding);
        }

        [Benchmark]
        public void OneWayToSourceBinding_Via_Binding()
        {
            var instance = new TestClass();

            var binding = new Binding(nameof(TestClass.BoundValue), BindingMode.OneWayToSource)
            {
                Source = instance
            };

            instance.Bind(TestClass.IntValueProperty, binding);
        }

        [Benchmark]
        public void UpdateTwoWayBinding_Via_Binding()
        {
            var instance = new TestClass();

            var binding = new Binding(nameof(TestClass.BoundValue), BindingMode.TwoWay)
            {
                Source = instance
            };

            instance.Bind(TestClass.IntValueProperty, binding);
            for (int i = 0; i < 60; i++)
            {
                instance.IntValue = i;
            }
        }
        private class TestClass : AvaloniaObject
        {
            public static readonly StyledProperty<int> IntValueProperty =
                AvaloniaProperty.Register<TestClass, int>(nameof(IntValue));

            public static readonly StyledProperty<int> BoundValueProperty =
                AvaloniaProperty.Register<TestClass, int>(nameof(BoundValue));

            public int IntValue
            {
                get => GetValue(IntValueProperty);
                set => SetValue(IntValueProperty, value);
            }

            public int BoundValue
            {
                get => GetValue(BoundValueProperty);
                set => SetValue(BoundValueProperty, value);
            }
        }
    }
}
