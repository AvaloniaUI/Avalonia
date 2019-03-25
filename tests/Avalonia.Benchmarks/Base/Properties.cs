using System.Reactive.Subjects;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObjectBenchmark
    {
        private Class1 target = new Class1();
        private Subject<int> intBinding = new Subject<int>();

        public AvaloniaObjectBenchmark()
        {
            target.SetValue(Class1.IntProperty, 123);
        }

        [Benchmark]
        public void ClearAndSetIntProperty()
        {
            target.ClearValue(Class1.IntProperty);
            target.SetValue(Class1.IntProperty, 123);
        }

        [Benchmark]
        public void BindIntProperty()
        {
            using (target.Bind(Class1.IntProperty, intBinding))
            {
                for (var i = 0; i < 100; ++i)
                {
                    intBinding.OnNext(i);
                }
            }
        }

        class Class1 : AvaloniaObject
        {
            public static readonly AvaloniaProperty<int> IntProperty =
                AvaloniaProperty.Register<Class1, int>("Int");
        }
    }
}
