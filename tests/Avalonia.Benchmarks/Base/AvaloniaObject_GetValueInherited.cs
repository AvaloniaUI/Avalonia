using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObject_GetValueInherited
    {
        private TestClass _root = null!;
        private TestClass _target = null!;

        public AvaloniaObject_GetValueInherited()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [Params(1, 2, 10, 50, 100, 200)]
        public int Depth { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _root = new();
            _root.SetValue(TestClass.StringProperty, "foo");
            _root.SetValue(TestClass.Struct1Property, new(1));
            _root.SetValue(TestClass.Struct2Property, new(1));
            _root.SetValue(TestClass.Struct3Property, new(1));
            _root.SetValue(TestClass.Struct4Property, new(1));
            _root.SetValue(TestClass.Struct5Property, new(1));
            _root.SetValue(TestClass.Struct6Property, new(1));
            _root.SetValue(TestClass.Struct7Property, new(1));
            _root.SetValue(TestClass.Struct8Property, new(1));

            var parent = _root;

            for (var i = 0; i < Depth; ++i)
            {
                var c = new TestClass();
                ((ISetLogicalParent)c).SetParent(parent);
                parent = c;
            }

            _target = parent;
        }

        [Benchmark]
        public int GetInheritedValues()
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

        private class TestClass : Control
        {
            public static readonly StyledProperty<string> StringProperty =
                AvaloniaProperty.Register<TestClass, string>("String", inherits: true);
            public static readonly StyledProperty<Struct1> Struct1Property =
                AvaloniaProperty.Register<TestClass, Struct1>("Struct1", inherits: true);
            public static readonly StyledProperty<Struct2> Struct2Property =
                AvaloniaProperty.Register<TestClass, Struct2>("Struct2", inherits: true);
            public static readonly StyledProperty<Struct3> Struct3Property =
                AvaloniaProperty.Register<TestClass, Struct3>("Struct3", inherits: true);
            public static readonly StyledProperty<Struct4> Struct4Property =
                AvaloniaProperty.Register<TestClass, Struct4>("Struct4", inherits: true);
            public static readonly StyledProperty<Struct5> Struct5Property =
                AvaloniaProperty.Register<TestClass, Struct5>("Struct5", inherits: true);
            public static readonly StyledProperty<Struct6> Struct6Property =
                AvaloniaProperty.Register<TestClass, Struct6>("Struct6", inherits: true);
            public static readonly StyledProperty<Struct7> Struct7Property =
                AvaloniaProperty.Register<TestClass, Struct7>("Struct7", inherits: true);
            public static readonly StyledProperty<Struct8> Struct8Property =
                AvaloniaProperty.Register<TestClass, Struct8>("Struct8", inherits: true);
        }
    }
}
