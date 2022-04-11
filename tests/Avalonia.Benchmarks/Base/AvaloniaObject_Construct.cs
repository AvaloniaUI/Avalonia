using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObject_Construct
    {
        public AvaloniaObject_Construct()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [Benchmark(Baseline = true)]
        public void ConstructClrObject_And_Set_Values()
        {
            var target = new BaselineTestClass();
            target.StringProperty = "foo";
            target.Struct1Property = new(1);
            target.Struct2Property = new(1);
            target.Struct3Property = new(1);
            target.Struct4Property = new(1);
            target.Struct5Property = new(1);
            target.Struct6Property = new(1);
            target.Struct7Property = new(1);
            target.Struct8Property = new(1);
        }

        [Benchmark]
        public void Construct_And_Set_Values()
        {
            var target = new TestClass();
            target.SetValue(TestClass.StringProperty, "foo");
            target.SetValue(TestClass.Struct1Property, new(1));
            target.SetValue(TestClass.Struct2Property, new(1));
            target.SetValue(TestClass.Struct3Property, new(1));
            target.SetValue(TestClass.Struct4Property, new(1));
            target.SetValue(TestClass.Struct5Property, new(1));
            target.SetValue(TestClass.Struct6Property, new(1));
            target.SetValue(TestClass.Struct7Property, new(1));
            target.SetValue(TestClass.Struct8Property, new(1));
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
