// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_MultiBinding
    {
        [Fact]
        public void Should_Update()
        {
            var target = new Class1();

            var b = new Subject<object>();

            var mb = new MultiBinding()
            {
                Converter = StringJoinConverter,
                Bindings = new[]
                {
                    b.ToBinding()
                }
            };
            target.Bind(Class1.FooProperty, mb);

            Assert.Equal(null, target.Foo);

            b.OnNext("Foo");

            Assert.Equal("Foo", target.Foo);

            b.OnNext("Bar");

            Assert.Equal("Bar", target.Foo);
        }

        [Fact]
        public void Should_Update_With_Multiple_Bindings()
        {
            var target = new Class1();

            var bindings = Enumerable.Range(0, 3).Select(i => new BehaviorSubject<object>("Empty")).ToArray();

            var mb = new MultiBinding()
            {
                Converter = StringJoinConverter,
                Bindings = bindings.Select(b => b.ToBinding()).ToArray()
            };
            target.Bind(Class1.FooProperty, mb);

            Assert.Equal("Empty,Empty,Empty", target.Foo);

            bindings[0].OnNext("Foo");

            Assert.Equal("Foo,Empty,Empty", target.Foo);

            bindings[1].OnNext("Bar");

            Assert.Equal("Foo,Bar,Empty", target.Foo);

            bindings[2].OnNext("Baz");

            Assert.Equal("Foo,Bar,Baz", target.Foo);
        }

        [Fact]
        public void Should_Update_When_Null_Value_In_Bindings()
        {
            var target = new Class1();

            var b = new Subject<object>();

            var mb = new MultiBinding()
            {
                Converter = StringJoinConverter,
                Bindings = new[]
                {
                    b.ToBinding()
                }
            };
            target.Bind(Class1.FooProperty, mb);

            Assert.Equal(null, target.Foo);

            b.OnNext("Foo");

            Assert.Equal("Foo", target.Foo);

            b.OnNext(null);

            Assert.Equal("", target.Foo);
        }

        [Fact]
        public void Should_Update_When_Null_Value_In_Bindings_With_StringFormat()
        {
            var target = new Class1();

            var b = new Subject<object>();

            var mb = new MultiBinding()
            {
                StringFormat = "Converted: {0}",
                Bindings = new[]
                {
                    b.ToBinding()
                }
            };
            target.Bind(Class1.FooProperty, mb);

            Assert.Equal(null, target.Foo);
            b.OnNext("Foo");
            Assert.Equal("Converted: Foo", target.Foo);
            b.OnNext(null);
            Assert.Equal("Converted: ", target.Foo);
        }

        [Fact]
        public void MultiValueConverter_Should_Not_Skip_Valid_Null_ReferenceType_Value()
        {
            var target = new FuncMultiValueConverter<string, string>(v => string.Join(",", v.ToArray()));

            object value = target.Convert(new[] { "Foo", "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal("Foo,Bar,Baz", value);

            value = target.Convert(new[] { null, "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal(",Bar,Baz", value);
        }

        [Fact]
        public void MultiValueConverter_Should_Not_Skip_Valid_Default_ValueType_Value()
        {
            var target = new FuncMultiValueConverter<StringValueTypeWrapper, string>(v => string.Join(",", v.ToArray()));

            IList<object> create(string[] values) =>
                values.Select(v => (object)(v != null ? new StringValueTypeWrapper() { Value = v } : default)).ToList();

            object value = target.Convert(create(new[] { "Foo", "Bar", "Baz" }), typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal("Foo,Bar,Baz", value);

            value = target.Convert(create(new[] { null, "Bar", "Baz" }), typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal(",Bar,Baz", value);
        }

        private struct StringValueTypeWrapper
        {
            public string Value;

            public override string ToString() => Value;
        }

        private static IMultiValueConverter StringJoinConverter = new FuncMultiValueConverter<object, string>(v => string.Join(",", v.ToArray()));

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo");

            public string Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }
        }
    }
}
