using System.Runtime.CompilerServices;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObject_GetValue
    {
        private BaselineTestClass _baseline = new(){ StringProperty = "foo" };
        private TestClass _target = new();

        public AvaloniaObject_GetValue()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [Benchmark(Baseline = true)]
        public int GetClrPropertyValues()
        {
            var target = _baseline;
            var result = 0;

            for (var i = 0; i < 100; ++i)
            {
                result += target.StringProperty?.Length ?? 0;
                result += target.Struct1Property.Int1;
                result += target.Struct2Property.Int1;
                result += target.Struct3Property.Int1;
                result += target.Struct4Property.Int1;
                result += target.Struct5Property.Int1;
                result += target.Struct6Property.Int1;
                result += target.Struct7Property.Int1;
                result += target.Struct8Property.Int1;
            }

            return result;
        }

        [Benchmark]
        public int GetDefaultValues()
        {
            var target = _target;
            var result = 0;

            for (var i = 0; i < 100; ++i)
            {
                result += target.GetValue(TestClass.StringProperty)?.Length ?? 0;
                result += target.GetValue(TestClass.Struct1Property).Int1;
                result += target.GetValue(TestClass.Struct2Property).Int1;
                result += target.GetValue(TestClass.Struct3Property).Int1;
                result += target.GetValue(TestClass.Struct4Property).Int1;
                result += target.GetValue(TestClass.Struct5Property).Int1;
                result += target.GetValue(TestClass.Struct6Property).Int1;
                result += target.GetValue(TestClass.Struct7Property).Int1;
                result += target.GetValue(TestClass.Struct8Property).Int1;
            }

            return result;
        }

        [GlobalSetup(Target = nameof(Get_Local_Values))]
        public void SetupLocalValues()
        {
            _target.SetValue(TestClass.StringProperty, "foo");
            _target.SetValue(TestClass.Struct1Property, new(1));
            _target.SetValue(TestClass.Struct2Property, new(1));
            _target.SetValue(TestClass.Struct3Property, new(1));
            _target.SetValue(TestClass.Struct4Property, new(1));
            _target.SetValue(TestClass.Struct5Property, new(1));
            _target.SetValue(TestClass.Struct6Property, new(1));
            _target.SetValue(TestClass.Struct7Property, new(1));
            _target.SetValue(TestClass.Struct8Property, new(1));
        }

        [Benchmark]
        public int Get_Local_Values()
        {
            var target = _target;
            var result = 0;

            for (var i = 0; i < 100; ++i)
            {
                result += target.GetValue(TestClass.StringProperty)?.Length ?? 0;
                result += target.GetValue(TestClass.Struct1Property).Int1;
                result += target.GetValue(TestClass.Struct2Property).Int1;
                result += target.GetValue(TestClass.Struct3Property).Int1;
                result += target.GetValue(TestClass.Struct4Property).Int1;
                result += target.GetValue(TestClass.Struct5Property).Int1;
                result += target.GetValue(TestClass.Struct6Property).Int1;
                result += target.GetValue(TestClass.Struct7Property).Int1;
                result += target.GetValue(TestClass.Struct8Property).Int1;
            }

            return result;
        }

        [GlobalSetup(Target = nameof(Get_Local_Values_With_Style_Values))]
        public void SetupLocalValuesAndStyleValues()
        {
            var target = _target;
            target.SetValue(TestClass.StringProperty, "foo");
            target.SetValue(TestClass.Struct1Property, new(1));
            target.SetValue(TestClass.Struct2Property, new(1));
            target.SetValue(TestClass.Struct3Property, new(1));
            target.SetValue(TestClass.Struct4Property, new(1));
            target.SetValue(TestClass.Struct5Property, new(1));
            target.SetValue(TestClass.Struct6Property, new(1));
            target.SetValue(TestClass.Struct7Property, new(1));
            target.SetValue(TestClass.Struct8Property, new(1));
            target.SetValue(TestClass.StringProperty, "bar", BindingPriority.Style);
            target.SetValue(TestClass.Struct1Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct2Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct3Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct4Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct5Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct6Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct7Property, new(), BindingPriority.Style);
            target.SetValue(TestClass.Struct8Property, new(), BindingPriority.Style);
        }

        [Benchmark]
        public int Get_Local_Values_With_Style_Values()
        {
            var target = _target;
            var result = 0;

            for (var i = 0; i < 100; ++i)
            {
                result += target.GetValue(TestClass.StringProperty)?.Length ?? 0;
                result += target.GetValue(TestClass.Struct1Property).Int1;
                result += target.GetValue(TestClass.Struct2Property).Int1;
                result += target.GetValue(TestClass.Struct3Property).Int1;
                result += target.GetValue(TestClass.Struct4Property).Int1;
                result += target.GetValue(TestClass.Struct5Property).Int1;
                result += target.GetValue(TestClass.Struct6Property).Int1;
                result += target.GetValue(TestClass.Struct7Property).Int1;
                result += target.GetValue(TestClass.Struct8Property).Int1;
            }

            return result;
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

        private class BaselineTestClass
        {
            public string? StringProperty { get; set; }
            public Struct1 Struct1Property { get; set; }
            public Struct2 Struct2Property { get; set; }
            public Struct3 Struct3Property { get; set; }
            public Struct4 Struct4Property { get; set; }
            public Struct5 Struct5Property { get; set; }
            public Struct6 Struct6Property { get; set; }
            public Struct7 Struct7Property { get; set; }
            public Struct8 Struct8Property { get; set; }
        }
    }
}
