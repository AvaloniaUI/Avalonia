using Avalonia.Data;
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
