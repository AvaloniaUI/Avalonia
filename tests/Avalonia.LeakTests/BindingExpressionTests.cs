using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.LeakTests
{
    public class BindingExpressionTests : ScopedTestBase
    {
        [Fact]
        public void Should_Not_Keep_Source_Alive_ObservableCollection()
        {
            static WeakReference CreateExpression()
            {
                var list = new AvaloniaList<string> { "foo", "bar" };
                var source = new { Foo = list };
                var target = BindingExpression.Create(source, o => o.Foo);

                target.ToObservable().Subscribe(_ => { });
                return new WeakReference(list);
            }

            var weakSource = CreateExpression();
            Assert.True(weakSource.IsAlive);

            GC.Collect();

            Assert.False(weakSource.IsAlive);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_ObservableCollection_With_DataValidation()
        {
            static WeakReference CreateExpression()
            {
                var list = new AvaloniaList<string> { "foo", "bar" };
                var source = new { Foo = list };
                var target = BindingExpression.Create(source, o => o.Foo, enableDataValidation: true);

                target.ToObservable().Subscribe(_ => { });
                return new WeakReference(list);
            }

            var weakSource = CreateExpression();
            Assert.True(weakSource.IsAlive);

            GC.Collect();

            Assert.False(weakSource.IsAlive);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_NonIntegerIndexer()
        {
            static WeakReference CreateExpression()
            {
                var indexer = new NonIntegerIndexer();
                var source = new { Foo = indexer };
                var target = BindingExpression.Create(source, o => o.Foo);

                target.ToObservable().Subscribe(_ => { });
                return new WeakReference(indexer);
            }

            var weakSource = CreateExpression();
            Assert.True(weakSource.IsAlive);

            GC.Collect();

            Assert.False(weakSource.IsAlive);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_MethodBinding()
        {
            static WeakReference CreateExpression()
            {
                var methodBound = new MethodBound();
                var source = new { Foo = methodBound };
                var target = BindingExpression.Create(source, o => (Action)o.Foo.A);
                target.ToObservable().Subscribe(_ => { });
                return new WeakReference(methodBound);
            }

            var weakSource = CreateExpression();
            Assert.True(weakSource.IsAlive);

            GC.Collect();

            Assert.False(weakSource.IsAlive);
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
