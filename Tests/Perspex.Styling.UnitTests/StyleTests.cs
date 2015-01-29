// -----------------------------------------------------------------------
// <copyright file="StyleTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Perspex.Styling;

    ////[TestFixture]
    ////public class StyleTests
    ////{
    ////    [Fact]
    ////    public void Style_With_Only_Type_Selector_Should_Update_Value()
    ////    {
    ////        Style style = new Style(x => x.OfType<Class1>())
    ////        {
    ////            Setters = new[]
    ////            {
    ////                new Setter(Class1.FooProperty, "Foo"),
    ////            },
    ////        };

    ////        var target = new Class1();

    ////        style.Attach(target);

    ////        Assert.Equal("Foo", target.Foo);
    ////    }

    ////    [Fact]
    ////    public void Style_With_Class_Selector_Should_Update_And_Restore_Value()
    ////    {
    ////        Style style = new Style(x => x.OfType<Class1>().Class("foo"))
    ////        {
    ////            Setters = new[]
    ////            {
    ////                new Setter(Class1.FooProperty, "Foo"),
    ////            },
    ////        };

    ////        var target = new Class1();

    ////        style.Attach(target);
    ////        Assert.Equal("foodefault", target.Foo);
    ////        target.Classes.Add("foo");
    ////        Assert.Equal("Foo", target.Foo);
    ////        target.Classes.Remove("foo");
    ////        Assert.Equal("foodefault", target.Foo);
    ////    }

    ////    [Fact]
    ////    public void LocalValue_Should_Override_Style()
    ////    {
    ////        Style style = new Style(x => x.OfType<Class1>())
    ////        {
    ////            Setters = new[]
    ////            {
    ////                new Setter(Class1.FooProperty, "Foo"),
    ////            },
    ////        };

    ////        var target = new Class1
    ////        {
    ////            Foo = "Original",
    ////        };

    ////        style.Attach(target);
    ////        Assert.Equal("Original", target.Foo);
    ////    }

    ////    [Fact]
    ////    public void Later_Styles_Should_Override_Earlier()
    ////    {
    ////        Styles styles = new Styles
    ////        {
    ////            new Style(x => x.OfType<Class1>().Class("foo"))
    ////            {
    ////                Setters = new[]
    ////                {
    ////                    new Setter(Class1.FooProperty, "Foo"),
    ////                },
    ////            },

    ////            new Style(x => x.OfType<Class1>().Class("foo"))
    ////            {
    ////                Setters = new[]
    ////                {
    ////                    new Setter(Class1.FooProperty, "Bar"),
    ////                },
    ////            }
    ////        };

    ////        var target = new Class1();

    ////        List<string> values = new List<string>();
    ////        target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

    ////        styles.Attach(target);
    ////        target.Classes.Add("foo");
    ////        target.Classes.Remove("foo");

    ////        Assert.Equal(new[] { "foodefault", "Foo", "Bar", "foodefault" }, values);
    ////    }

    ////    private class Class1 : Control
    ////    {
    ////        public static readonly PerspexProperty<string> FooProperty =
    ////            PerspexProperty.Register<Class1, string>("Foo", "foodefault");

    ////        public string Foo
    ////        {
    ////            get { return this.GetValue(FooProperty); }
    ////            set { this.SetValue(FooProperty, value); }
    ////        }

    ////        protected override Size MeasureOverride(Size availableSize)
    ////        {
    ////            throw new NotImplementedException();
    ////        }
    ////    }
    ////}
}
