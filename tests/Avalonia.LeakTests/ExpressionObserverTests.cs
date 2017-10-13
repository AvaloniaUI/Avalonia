using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class ExpressionObserverTests
    {
        public ExpressionObserverTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_ObservableCollection()
        {
            Func<ExpressionObserver> run = () =>
            {
                var source = new { Foo = new AvaloniaList<string> {"foo", "bar"} };
                var target = new ExpressionObserver(source, "Foo");

                target.Subscribe(_ => { });
                return target;
            };

            var result = run();

            dotMemory.Check(memory => 
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<AvaloniaList<string>>()).ObjectsCount));
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_ObservableCollection_With_DataValidation()
        {
            Func<ExpressionObserver> run = () =>
            {
                var source = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
                var target = new ExpressionObserver(source, "Foo", true);

                target.Subscribe(_ => { });
                return target;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<AvaloniaList<string>>()).ObjectsCount));
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_NonIntegerIndexer()
        {
            Func<ExpressionObserver> run = () =>
            {
                var source = new { Foo = new NonIntegerIndexer() };
                var target = new ExpressionObserver(source, "Foo");

                target.Subscribe(_ => { });
                return target;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<NonIntegerIndexer>()).ObjectsCount));
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_MethodBinding()
        {
            Func<ExpressionObserver> run = () =>
            {
                var source = new { Foo = new MethodBound() };
                var target = new ExpressionObserver(source, "Foo.A");
                target.Subscribe(_ => { });
                return target;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<MethodBound>()).ObjectsCount));
        }

        private class MethodBound
        {
            public void A() { }
        }

        private class NonIntegerIndexer : NotifyingBase
        {
            private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

            public string this[string key]
            {
                get
                {
                    return _storage[key];
                }
                set
                {
                    _storage[key] = value;
                    RaisePropertyChanged(CommonPropertyNames.IndexerName);
                }
            }
        }
    }
}
