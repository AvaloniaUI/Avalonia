// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Perspex.Markup.Binding;
using Xunit;

namespace Perspex.Markup.UnitTests.Binding
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
        public async void Array_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
        }

        [Fact]
        public async void Array_With_Wrong_Dimensions_Should_Return_UnsetValue()
        {
            var data = new { Foo = new[] { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1,2]");
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
        }

        [Fact]
        public async void List_Out_Of_Bounds_Should_Return_UnsetValue()
        {
            var data = new { Foo = new List<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
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
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[2]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Add("baz");

            Assert.Equal(new[] { PerspexProperty.UnsetValue, "baz" }, result);
        }

        [Fact]
        public void Should_Track_INCC_Remove()
        {
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[0]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.RemoveAt(0);

            Assert.Equal(new[] { "foo", "bar" }, result);
        }

        [Fact]
        public void Should_Track_INCC_Replace()
        {
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo[1] = "baz";

            Assert.Equal(new[] { "bar", "baz" }, result);
        }

        [Fact]
        public void Should_Track_INCC_Move()
        {
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
            var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
            var target = new ExpressionObserver(data, "Foo[1]");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo.Clear();

            Assert.Equal(new[] { "bar", PerspexProperty.UnsetValue }, result);
        }
    }
}
