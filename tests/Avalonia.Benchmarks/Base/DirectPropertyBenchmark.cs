using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class DirectPropertyBenchmark
    {
        [Benchmark(Baseline = true)]
        public void SetAndRaiseOriginal()
        {
            var obj = new DirectClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.IntValue += 1;
            }
        }

        [Benchmark]
        public void SetAndRaiseSimple()
        {
            var obj = new DirectClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.IntValueSimple += 1;
            }
        }

        class DirectClass : AvaloniaObject
        {
            private int _intValue;

            public static readonly DirectProperty<DirectClass, int> IntValueProperty =
                AvaloniaProperty.RegisterDirect<DirectClass, int>(nameof(IntValue),
                    o => o.IntValue,
                    (o, v) => o.IntValue = v);

            public int IntValue
            {
                get => _intValue;
                set => SetAndRaise(IntValueProperty, ref _intValue, value);
            }

            public int IntValueSimple
            {
                get => _intValue;
                set
                {
                    VerifyAccess();

                    if (_intValue == value)
                    {
                        return;
                    }

                    var old = _intValue;
                    _intValue = value;

                    RaisePropertyChanged(IntValueProperty, old, _intValue);
                }
            }
        }
    }
}
