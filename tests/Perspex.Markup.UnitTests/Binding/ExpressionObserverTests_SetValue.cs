// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Perspex.Markup.Data;
using Xunit;

namespace Perspex.Markup.UnitTests.Binding
{
    public class ExpressionObserverTests_SetValue
    {
        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            target.SetValue("bar");

            Assert.Equal("foo", data.Foo);
        }

        [Fact]
        public void Should_Set_Value_On_Simple_Property_Chain()
        {
            var data = new Class1 { Foo = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Foo.Bar");

            target.SetValue("foo");

            Assert.Equal("foo", data.Foo.Bar);
        }

        [Fact]
        public void Should_Not_Try_To_Set_Value_On_Broken_Chain()
        {
            var data = new Class1 { Foo = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Foo.Bar");

            // Ensure the ExpressionObserver's subscriptions are kept active.
            target.OfType<string>().Subscribe(x => { });

            data.Foo = null;

            Assert.False(target.SetValue("foo"));
        }

        private class Class1 : NotifyingBase
        {
            private Class2 _foo;

            public Class2 Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }
        }

        private class Class2 : NotifyingBase
        {
            private string _bar;

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
                }
            }
        }
    }
}
