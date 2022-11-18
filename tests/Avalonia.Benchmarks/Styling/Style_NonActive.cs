using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.PropertyStore;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class Style_NonActive
    {
        private TestClass _target = null!;

        public Style_NonActive()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [GlobalSetup]
        public void Setup()
        {
            _target = new TestClass();

            var style = new Style(x => x.OfType<TestClass>().Class("foo"))
            {
                Setters = { new Setter(TestClass.StringProperty, "foo") }
            };

            StyleHelpers.TryAttach(style, _target);
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
        public void Toggle_NonActive_Style_Activation()
        {
            for (var i = 0; i < 100; ++i)
            {
                _target.Classes.Add("foo");
                _target.Classes.Remove("foo");
            }
        }

        private class TestClass : Control
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
