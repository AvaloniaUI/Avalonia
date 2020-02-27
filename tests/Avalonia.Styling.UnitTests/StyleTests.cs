// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class StyleTests
    {
        [Fact]
        public void Style_With_Only_Type_Selector_Should_Update_Value()
        {
            Style style = new Style(x => x.OfType<Class1>())
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, null);

            Assert.Equal("Foo", target.Foo);
        }

        [Fact]
        public void Style_With_Class_Selector_Should_Update_And_Restore_Value()
        {
            Style style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, null);
            Assert.Equal("foodefault", target.Foo);
            target.Classes.Add("foo");
            Assert.Equal("Foo", target.Foo);
            target.Classes.Remove("foo");
            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Style_With_No_Selector_Should_Apply_To_Containing_Control()
        {
            Style style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            style.TryAttach(target, target);

            Assert.Equal("Foo", target.Foo);
        }

        [Fact]
        public void Style_With_No_Selector_Should_Not_Apply_To_Other_Control()
        {
            Style style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();
            var other = new Class1();

            style.TryAttach(target, other);

            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void LocalValue_Should_Override_Style()
        {
            Style style = new Style(x => x.OfType<Class1>())
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1
            {
                Foo = "Original",
            };

            style.TryAttach(target, null);
            Assert.Equal("Original", target.Foo);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Foo"),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Bar"),
                    },
                }
            };

            var target = new Class1();

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Foo", "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier_2()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Foo"),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "Bar"),
                    },
                }
            };

            var target = new Class1();

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("bar");
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier_3()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, new Binding("Foo")),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, new Binding("Bar")),
                    },
                }
            };

            var target = new Class1
            {
                DataContext = new
                {
                    Foo = "Foo",
                    Bar = "Bar",
                }
            };

            List<string> values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

            styles.TryAttach(target, null);
            target.Classes.Add("bar");
            target.Classes.Add("foo");
            target.Classes.Remove("foo");

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Style_Should_Detach_When_Control_Removed_From_Logical_Tree()
        {
            Border border;

            var style = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                }
            };

            var root = new TestRoot
            {
                Child = border = new Border(),
            };

            style.TryAttach(border, null);

            Assert.Equal(new Thickness(4), border.BorderThickness);
            root.Child = null;
            Assert.Equal(new Thickness(0), border.BorderThickness);
        }

        private class Class1 : Control
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                throw new NotImplementedException();
            }
        }
    }
}
