﻿using Avalonia.Collections;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Parsers.Nodes;
using Avalonia.UnitTests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Indexer
    {
        [Fact]
        public async Task Should_Get_Array_Value()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_UnsetValue_For_Invalid_Array_Index()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[invalid]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_UnsetValue_For_Invalid_Dictionary_Index()
        {
            var data = new { Foo = new Dictionary<int, string> { { 1, "foo" } } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[invalid]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_UnsetValue_For_Object_Without_Indexer()
        {
            var data = new { Foo = 5 };
            var target = ExpressionObserverBuilder.Build(data, "Foo[noindexer]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_MultiDimensional_Array_Value()
        {
            var data = new { Foo = new[,] { { "foo", "bar" }, { "baz", "qux" } } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1, 1]");
            var result = await target.Take(1);

            Assert.Equal("qux", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Value_For_String_Indexer()
        {
            var data = new { Foo = new Dictionary<string, string> { { "foo", "bar" }, { "baz", "qux" } } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[foo]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Value_For_Non_String_Indexer()
        {
            var data = new { Foo = new Dictionary<double, string> { { 1.0, "bar" }, { 2.0, "qux" } } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1.0]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Array_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Array_With_Wrong_Dimensions_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1,2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task List_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new List<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_List_Value()
        {
            var data = new { Foo = new List<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_INCC_Add()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[2]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo.Add("baz");
            }
            
            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            
            Assert.Equal(new[] { AvaloniaProperty.UnsetValue, "baz" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_INCC_Remove()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[0]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo.RemoveAt(0);
            }

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            
            Assert.Equal(new[] { "foo", "bar" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_INCC_Replace()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo[1] = "baz";
            }

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            
            Assert.Equal(new[] { "bar", "baz" }, result);
            Assert.Null(((INotifyCollectionChangedDebug)data.Foo).GetCollectionChangedSubscribers());

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_INCC_Move()
        {
            // Using ObservableCollection here because AvaloniaList does not yet have a Move
            // method, but even if it did we need to test with ObservableCollection as well
            // as AvaloniaList as it implements PropertyChanged as an explicit interface event.
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Move(0, 1);

            Assert.Equal(new[] { "bar", "foo" }, result);

            GC.KeepAlive(sub);
            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_INCC_Reset()
        {
            var data = new { Foo = new AvaloniaList<string> { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Clear();

            Assert.Equal(new[] { "bar", AvaloniaProperty.UnsetValue }, result);

            GC.KeepAlive(sub);
            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_NonIntegerIndexer()
        {
            var data = new { Foo = new NonIntegerIndexer() };
            data.Foo["foo"] = "bar";
            data.Foo["baz"] = "qux";

            var target = ExpressionObserverBuilder.Build(data, "Foo[foo]");
            var result = new List<object>();

            using (var sub = target.Subscribe(x => result.Add(x)))
            {
                data.Foo["foo"] = "bar2";
            }

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            
            var expected = new[] { "bar", "bar2" };
            Assert.Equal(expected, result);
            Assert.Equal(0, data.Foo.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_SetArrayIndex()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = ExpressionObserverBuilder.Build(data, "Foo[1]");

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("baz"));
            }

            Assert.Equal("baz", data.Foo[1]);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_ExistingDictionaryEntry()
        {
            var data = new
            {
                Foo = new Dictionary<string, int>
                {
                    {"foo", 1 }
                }
            };

            var target = ExpressionObserverBuilder.Build(data, "Foo[foo]");
            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue(4));
            }

            Assert.Equal(4, data.Foo["foo"]);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Add_NewDictionaryEntry()
        {
            var data = new
            {
                Foo = new Dictionary<string, int>
                {
                    {"foo", 1 }
                }
            };

            var target = ExpressionObserverBuilder.Build(data, "Foo[bar]");
            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue(4));
            }

            Assert.Equal(4, data.Foo["bar"]);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_NonIntegerIndexer()
        {
            var data = new { Foo = new NonIntegerIndexer() };
            data.Foo["foo"] = "bar";
            data.Foo["baz"] = "qux";

            var target = ExpressionObserverBuilder.Build(data, "Foo[foo]");

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("bar2"));
            }

            Assert.Equal("bar2", data.Foo["foo"]);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Indexer_Only_Binding_Works()
        {
            var data = new[] { 1, 2, 3 };

            var target = ExpressionObserverBuilder.Build(data, "[1]");

            var value = await target.Take(1);

            Assert.Equal(data[1], value);
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
