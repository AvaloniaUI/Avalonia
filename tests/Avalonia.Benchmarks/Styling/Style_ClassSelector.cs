﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Styling;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class Style_ClassSelector
    {
        private Style _style = null!;

        public Style_ClassSelector()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestClass).TypeHandle);
        }

        [GlobalSetup]
        public void Setup()
        {
            _style = new Style(x => x.OfType<TestClass>().Class("foo"))
            {
                Setters = { new Setter(TestClass.StringProperty, "foo") }
            };
        }

        [Benchmark(OperationsPerInvoke = 50)]
        public void Apply()
        {
            var target = new TestClass();

            target.BeginBatchUpdate();

            for (var i = 0; i < 50; ++i)
                _style.TryAttach(target, null);

            target.EndBatchUpdate();
        }

        [Benchmark(OperationsPerInvoke = 50)]
        public void Apply_Toggle()
        {
            var target = new TestClass();

            target.BeginBatchUpdate();

            for (var i = 0; i < 50; ++i)
                _style.TryAttach(target, null);

            target.EndBatchUpdate();

            target.Classes.Add("foo");
            target.Classes.Remove("foo");
        }

        [Benchmark(OperationsPerInvoke = 50)]
        public void Apply_Detach()
        {
            var target = new TestClass();

            target.BeginBatchUpdate();

            for (var i = 0; i < 50; ++i)
                _style.TryAttach(target, null);

            target.EndBatchUpdate();

            target.DetachStyles();
        }

        private class TestClass : Control
        {
            public static readonly StyledProperty<string?> StringProperty =
                AvaloniaProperty.Register<TestClass, string?>("String");
            public void DetachStyles() => InvalidateStyles();
        }

        private class TestClass2 : Control
        {
        }
    }
}
