using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObject_GetObservable
    {
        private TestClass _target = null!;
        public static int result;

        public AvaloniaObject_GetObservable()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [GlobalSetup]
        public void Setup()
        {
            _target = new();
        }

        [Benchmark(Baseline = true)]
        public void PropertyChangedSubscription()
        {
            var target = _target;

            static void ChangeHandler(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == TestClass.StringProperty)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<string?>)e;
                    result += ev.NewValue.Value?.Length ?? 0;
                }
                else if (e.Property == TestClass.Struct1Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct1>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct2Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct2>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct3Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct3>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct4Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct4>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct5Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct5>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct6Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct6>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct7Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct7>)e;
                    result += ev.NewValue.Value.Int1;
                }
                else if (e.Property == TestClass.Struct8Property)
                {
                    var ev = (AvaloniaPropertyChangedEventArgs<Struct8>)e;
                    result += ev.NewValue.Value.Int1;
                }
            }

            target.PropertyChanged += ChangeHandler;

            // GetObservable fires with the initial value so to compare like-for-like we also need
            // to get the initial value here.
            result += target.GetValue(TestClass.StringProperty)?.Length ?? 0;
            result += target.GetValue(TestClass.Struct1Property).Int1;
            result += target.GetValue(TestClass.Struct2Property).Int1;
            result += target.GetValue(TestClass.Struct3Property).Int1;
            result += target.GetValue(TestClass.Struct4Property).Int1;
            result += target.GetValue(TestClass.Struct5Property).Int1;
            result += target.GetValue(TestClass.Struct6Property).Int1;
            result += target.GetValue(TestClass.Struct7Property).Int1;
            result += target.GetValue(TestClass.Struct8Property).Int1;

            for (var i = 0; i < 100; ++i)
            {
                target.SetValue(TestClass.StringProperty, "foo" + i);
                target.SetValue(TestClass.Struct1Property, new(i + 1));
                target.SetValue(TestClass.Struct2Property, new(i + 1));
                target.SetValue(TestClass.Struct3Property, new(i + 1));
                target.SetValue(TestClass.Struct4Property, new(i + 1));
                target.SetValue(TestClass.Struct5Property, new(i + 1));
                target.SetValue(TestClass.Struct6Property, new(i + 1));
                target.SetValue(TestClass.Struct7Property, new(i + 1));
                target.SetValue(TestClass.Struct8Property, new(i + 1));
            }

            target.PropertyChanged -= ChangeHandler;
        }

        [Benchmark]
        public void GetObservables()
        {
            var target = _target;

            var sub1 = target.GetObservable(TestClass.StringProperty).Subscribe(x => result += x?.Length ?? 0);
            var sub2 = target.GetObservable(TestClass.Struct1Property).Subscribe(x => result += x.Int1);
            var sub3 = target.GetObservable(TestClass.Struct2Property).Subscribe(x => result += x.Int1);
            var sub4 = target.GetObservable(TestClass.Struct3Property).Subscribe(x => result += x.Int1);
            var sub5 = target.GetObservable(TestClass.Struct4Property).Subscribe(x => result += x.Int1);
            var sub6 = target.GetObservable(TestClass.Struct5Property).Subscribe(x => result += x.Int1);
            var sub7 = target.GetObservable(TestClass.Struct6Property).Subscribe(x => result += x.Int1);
            var sub8 = target.GetObservable(TestClass.Struct7Property).Subscribe(x => result += x.Int1);
            var sub9 = target.GetObservable(TestClass.Struct8Property).Subscribe(x => result += x.Int1);

            for (var i = 0; i < 100; ++i)
            {
                target.SetValue(TestClass.StringProperty, "foo" + i);
                target.SetValue(TestClass.Struct1Property, new(i + 1));
                target.SetValue(TestClass.Struct2Property, new(i + 1));
                target.SetValue(TestClass.Struct3Property, new(i + 1));
                target.SetValue(TestClass.Struct4Property, new(i + 1));
                target.SetValue(TestClass.Struct5Property, new(i + 1));
                target.SetValue(TestClass.Struct6Property, new(i + 1));
                target.SetValue(TestClass.Struct7Property, new(i + 1));
                target.SetValue(TestClass.Struct8Property, new(i + 1));
            }

            sub1.Dispose();
            sub2.Dispose();
            sub3.Dispose();
            sub4.Dispose();
            sub5.Dispose();
            sub6.Dispose();
            sub7.Dispose();
            sub8.Dispose();
            sub9.Dispose();
        }

        private class TestClass : AvaloniaObject
        {
            public static readonly StyledProperty<string> StringProperty =
                AvaloniaProperty.Register<TestClass, string>("String");
            public static readonly StyledProperty<Struct1> Struct1Property =
                AvaloniaProperty.Register<TestClass, Struct1>("Struct1");
            public static readonly StyledProperty<Struct2> Struct2Property =
                AvaloniaProperty.Register<TestClass, Struct2>("Struct2");
            public static readonly StyledProperty<Struct3> Struct3Property =
                AvaloniaProperty.Register<TestClass, Struct3>("Struct3");
            public static readonly StyledProperty<Struct4> Struct4Property =
                AvaloniaProperty.Register<TestClass, Struct4>("Struct4");
            public static readonly StyledProperty<Struct5> Struct5Property =
                AvaloniaProperty.Register<TestClass, Struct5>("Struct5");
            public static readonly StyledProperty<Struct6> Struct6Property =
                AvaloniaProperty.Register<TestClass, Struct6>("Struct6");
            public static readonly StyledProperty<Struct7> Struct7Property =
                AvaloniaProperty.Register<TestClass, Struct7>("Struct7");
            public static readonly StyledProperty<Struct8> Struct8Property =
                AvaloniaProperty.Register<TestClass, Struct8>("Struct8");
        }
    }
}
