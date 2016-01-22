// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Subjects;
using Perspex.Data;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class ActivatedSubjectTests
    {
        [Fact]
        public void Should_Set_Values()
        {
            var data = new Class1 { Foo = "foo" };
            var activator = new BehaviorSubject<bool>(false);
            var source = data.GetSubject(
                (PerspexProperty)Class1.FooProperty, 
                BindingPriority.LocalValue);
            var target = new ActivatedSubject(activator, source, string.Empty);

            target.OnNext("bar");
            Assert.Equal("foo", data.Foo);
            activator.OnNext(true);
            target.OnNext("baz");
            Assert.Equal("baz", data.Foo);
            activator.OnNext(false);
            Assert.Equal("foo", data.Foo);
            target.OnNext("bax");
            activator.OnNext(true);
            Assert.Equal("bax", data.Foo);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "foodefault");

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }
        }
    }
}
