// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Diagnostics;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Indexer
    {
        [Fact]
        public async void Should_Get_Array_Value()
        {
            var data = new { Foo = new [] { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);
        }

        [Fact]
        public async void Should_Get_MultiDimensional_Array_Value()
        {
            var data = new { Foo = new[,] { { "foo", "bar" }, { "baz", "qux" } } };
            var target = new ExpressionObserver(data, "Foo[1, 1]");
            var result = await target.Take(1);

            Assert.Equal("qux", result);
        }

        [Fact]
        public async void Should_Get_Value_For_String_Indexer()
        {
            var data = new { Foo = new Dictionary<string, string> { { "foo", "bar" }, { "baz", "qux" } } };
            var target = new ExpressionObserver(data, "Foo[foo]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);
        }

        [Fact]
        public async void Should_Get_Value_For_Non_String_Indexer()
        {
            var data = new { Foo = new Dictionary<double, string> { { 1.0, "bar" }, { 2.0, "qux" } } };
            var target = new ExpressionObserver(data, "Foo[1.0]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);
        }

        [Fact]
        public async void Array_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);
        }

        [Fact]
        public async void Array_With_Wrong_Dimensions_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1,2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);
        }

        [Fact]
        public async void List_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new List<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);
        }

        [Fact]
        public async void Should_Get_List_Value()
        {
            var data = new { Foo = new List<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);
        }

        [Fact]
        public void Should_Track_INCC_Add()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo.Add("baz");
            }

            Assert.Equal(new[] { AvaloniaProperty.UnsetValue, "baz" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Should_Track_INCC_Remove()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[0]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo.RemoveAt(0);
            }

            Assert.Equal(new[] { "foo", "bar" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Should_Track_INCC_Replace()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo[1] = "baz";
            }

            Assert.Equal(new[] { "bar", "baz" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Should_Track_INCC_Move()
        {
            // Using ObservableCollection here because AvaloniaList does not yet have a Move
            // method, but even if it did we need to test with ObservableCollection as well
            // as AvaloniaList as it implements PropertyChanged as an explicit interface event.
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Move(0, 1);

            Assert.Equal(new[] { "bar", "foo" }, result);
        }

        [Fact]
        public void Should_Track_INCC_Reset()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Clear();

            Assert.Equal(new[] { "bar", AvaloniaProperty.UnsetValue }, result);
        }

        [Fact]
        public void Should_Track_NonIntegerIndexer()
        {
            var data = new { Foo = new NonIntegerIndexer() };
            data.Foo["foo"] = "bar";
            data.Foo["baz"] = "qux";

            var target = new ExpressionObserver(data, "Foo[foo]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo["foo"] = "bar2";
            }

            var expected = new[] { "bar", "bar2" };
            Assert.Equal(expected, result);
            Assert.Equal(0, data.Foo.SubscriptionCount);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_ObservableCollection()
        {
            Func<Tuple<ExpressionObserver, WeakReference>> run = () =>
            {
                var source = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
                var target = new ExpressionObserver(source, "Foo");
                return Tuple.Create(target, new WeakReference(source.Foo));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive_NonIntegerIndexer()
        {
            Func<Tuple<ExpressionObserver, WeakReference>> run = () =>
            {
                var source = new NonIntegerIndexer();
                var target = new ExpressionObserver(source, "Foo");
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        private class NonIntegerIndexer : NotifyingBase
        {
            private Dictionary<string, string> storage = new Dictionary<string, string>();

            public string this[string key]
            {
                get
                {
                    return storage[key];
                }
                set
                {
                    storage[key] = value;
                    RaisePropertyChanged(CommonPropertyNames.IndexerName);
                }
            }
        }
    }
}
