using System.Runtime.CompilerServices;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObject_Binding
    {
        private static TestClass _target = null!;
        private static TestBindingObservable<string?> s_stringSource = new();
        private static TestBindingObservable<Struct1> s_struct1Source = new();
        private static TestBindingObservable<Struct2> s_struct2Source = new();
        private static TestBindingObservable<Struct3> s_struct3Source = new();
        private static TestBindingObservable<Struct4> s_struct4Source = new();
        private static TestBindingObservable<Struct5> s_struct5Source = new();
        private static TestBindingObservable<Struct6> s_struct6Source = new();
        private static TestBindingObservable<Struct7> s_struct7Source = new();
        private static TestBindingObservable<Struct8> s_struct8Source = new();

        public AvaloniaObject_Binding()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [GlobalSetup]
        public void Setup()
        {
            _target = new TestClass();
        }

        [Benchmark]
        public void Setup_Dispose_LocalValue_Bindings()
        {
            var target = _target;

            for (var i = 0; i < 100; ++i)
            {
                using var s0 = target.Bind(TestClass.StringProperty, s_stringSource);
                using var s1 = target.Bind(TestClass.Struct1Property, s_struct1Source);
                using var s2 = target.Bind(TestClass.Struct2Property, s_struct2Source);
                using var s3 = target.Bind(TestClass.Struct3Property, s_struct3Source);
                using var s4 = target.Bind(TestClass.Struct4Property, s_struct4Source);
                using var s5 = target.Bind(TestClass.Struct5Property, s_struct5Source);
                using var s6 = target.Bind(TestClass.Struct6Property, s_struct6Source);
                using var s7 = target.Bind(TestClass.Struct7Property, s_struct7Source);
                using var s8 = target.Bind(TestClass.Struct8Property, s_struct8Source);
            }
        }


        [Benchmark]
        public void Fire_LocalValue_Bindings()
        {
            var target = _target;

            using var s0 = target.Bind(TestClass.StringProperty, s_stringSource);
            using var s1 = target.Bind(TestClass.Struct1Property, s_struct1Source);
            using var s2 = target.Bind(TestClass.Struct2Property, s_struct2Source);
            using var s3 = target.Bind(TestClass.Struct3Property, s_struct3Source);
            using var s4 = target.Bind(TestClass.Struct4Property, s_struct4Source);
            using var s5 = target.Bind(TestClass.Struct5Property, s_struct5Source);
            using var s6 = target.Bind(TestClass.Struct6Property, s_struct6Source);
            using var s7 = target.Bind(TestClass.Struct7Property, s_struct7Source);
            using var s8 = target.Bind(TestClass.Struct8Property, s_struct8Source);

            for (var i = 0; i < 100; ++i)
            {
                s_stringSource.OnNext(i.ToString());
                s_struct1Source.OnNext(new(i + 1));
                s_struct2Source.OnNext(new(i + 1));
                s_struct3Source.OnNext(new(i + 1));
                s_struct4Source.OnNext(new(i + 1));
                s_struct5Source.OnNext(new(i + 1));
                s_struct6Source.OnNext(new(i + 1));
                s_struct7Source.OnNext(new(i + 1));
                s_struct8Source.OnNext(new(i + 1));
            }
        }

        [GlobalSetup(Target = nameof(Fire_LocalValue_Bindings_With_Style_Values))]
        public void SetupStyleValues()
        {
            _target = new TestClass();
            _target.SetValue(TestClass.StringProperty, "foo", BindingPriority.Style);
            _target.SetValue(TestClass.Struct1Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct2Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct3Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct4Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct5Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct6Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct7Property, new(), BindingPriority.Style);
            _target.SetValue(TestClass.Struct8Property, new(), BindingPriority.Style);
        }

        [Benchmark]
        public void Fire_LocalValue_Bindings_With_Style_Values()
        {
            var target = _target;

            using var s0 = target.Bind(TestClass.StringProperty, s_stringSource);
            using var s1 = target.Bind(TestClass.Struct1Property, s_struct1Source);
            using var s2 = target.Bind(TestClass.Struct2Property, s_struct2Source);
            using var s3 = target.Bind(TestClass.Struct3Property, s_struct3Source);
            using var s4 = target.Bind(TestClass.Struct4Property, s_struct4Source);
            using var s5 = target.Bind(TestClass.Struct5Property, s_struct5Source);
            using var s6 = target.Bind(TestClass.Struct6Property, s_struct6Source);
            using var s7 = target.Bind(TestClass.Struct7Property, s_struct7Source);
            using var s8 = target.Bind(TestClass.Struct8Property, s_struct8Source);

            for (var i = 0; i < 100; ++i)
            {
                s_stringSource.OnNext(i.ToString());
                s_struct1Source.OnNext(new(i + 1));
                s_struct2Source.OnNext(new(i + 1));
                s_struct3Source.OnNext(new(i + 1));
                s_struct4Source.OnNext(new(i + 1));
                s_struct5Source.OnNext(new(i + 1));
                s_struct6Source.OnNext(new(i + 1));
                s_struct7Source.OnNext(new(i + 1));
                s_struct8Source.OnNext(new(i + 1));
            }
        }

        private class TestClass : AvaloniaObject
        {
            public static readonly StyledProperty<string?> StringProperty =
                AvaloniaProperty.Register<TestClass, string?>("String");
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
