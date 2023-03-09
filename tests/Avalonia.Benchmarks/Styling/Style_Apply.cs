using System;
using System.Collections.Generic;
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
    public class Style_Apply
    {
        private List<Style> _styles = new();

        public Style_Apply()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [Params(1, 5, 50)]
        public int MatchingStyles { get; set; }


        [Params(1, 5, 50)]
        public int NonMatchingStyles { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _styles.Clear();

            for (var i = 0; i < MatchingStyles; ++i)
            {
                _styles.Add(new Style(x => x.OfType<TestClass>())
                {
                    Setters = { new Setter(TestClass.StringProperty, "foo") }
                });
            }

            for (var i = 0; i < NonMatchingStyles; ++i)
            {
                _styles.Add(new Style(x => x.OfType<TestClass2>().Class("missing"))
                {
                    Setters = { new Setter(TestClass.StringProperty, "foo") }
                });
            }
        }

        [Benchmark]
        public void Apply_Simple_Styles()
        {
            var target = new TestClass();

            target.GetValueStore().BeginStyling();

            foreach (var style in _styles)
                StyleHelpers.TryAttach(style, target);

            target.GetValueStore().EndStyling();
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

        private class TestClass2 : Control
        {
        }
    }
}
