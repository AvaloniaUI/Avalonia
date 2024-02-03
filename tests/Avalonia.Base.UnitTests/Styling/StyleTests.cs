using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Base.UnitTests.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class StyleTests : ScopedTestBase
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

            StyleHelpers.TryAttach(style, target);

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

            StyleHelpers.TryAttach(style, target);
            Assert.Equal("foodefault", target.Foo);
            target.Classes.Add("foo");
            Assert.Equal("Foo", target.Foo);
            target.Classes.Remove("foo");
            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Style_With_Class_Selector_Should_Update_And_Restore_Value_With_TemplateBinding()
        {
            Style style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var templatedParent = new Class1 { Foo = "unset-foo" };
            var target = new Class1 { TemplatedParent = templatedParent };
            target.Bind(Class1.FooProperty, new TemplateBinding(Class1.FooProperty), BindingPriority.Template);

            StyleHelpers.TryAttach(style, target);
            Assert.Equal("unset-foo", target.Foo);
            target.Classes.Add("foo");
            Assert.Equal("Foo", target.Foo);
            target.Classes.Remove("foo");
            Assert.Equal("unset-foo", target.Foo);
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

            StyleHelpers.TryAttach(style, target);

            Assert.Equal("Foo", target.Foo);
        }

        [Fact]
        public void Should_Throw_For_Selector_With_Trailing_Template_Selector()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new Style(x => x.OfType<Button>().Template()));
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

            StyleHelpers.TryAttach(style, target, host: other);

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

            StyleHelpers.TryAttach(style, target);
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

            Assert.Equal(new[] { "foodefault", "Bar", "foodefault" }, values);
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
        public void Later_Styles_Should_Override_Earlier_4()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "foo1"),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "foo2"),
                        new Setter(Class1.DoubleProperty, 123.4),
                    },
                }
            };

            var target = new Class1();
            styles.TryAttach(target, null);
            target.Classes.Add("foo");

            Assert.Equal("foo2", target.Foo);
            Assert.Equal(123.4, target.Double);
        }

        [Fact]
        public void Later_Styles_Should_Override_Earlier_With_Begin_End_Styling()
        {
            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "foo1"),
                        new Setter(Class1.DoubleProperty, 123.4),
                    },
                },

                new Style(x => x.OfType<Class1>().Class("foo").Class("bar"))
                {
                    Setters =
                    {
                        new Setter(Class1.FooProperty, "foo2"),
                    },
                },
            };

            var target = new Class1();
            target.GetValueStore().BeginStyling();
            styles.TryAttach(target, null);
            target.GetValueStore().EndStyling();
            target.Classes.Add("bar");
            target.Classes.Add("foo");

            Assert.Equal("foo2", target.Foo);
            Assert.Equal(123.4, target.Double);

            target.Classes.Remove("foo");

            Assert.Equal(0, target.Double);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Attach()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var values = new List<string>();
            var target = new Class1();

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = target;

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Inactive_Bindings_Should_Not_Be_Made_Active_During_Style_Attach()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Foo")),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Bar")),
                        },
                    }
                }
            };

            var values = new List<string>();
            var target = new Class1
            {
                DataContext = new
                {
                    Foo = "Foo",
                    Bar = "Bar",
                }
            };

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = target;

            Assert.Equal(new[] { "foodefault", "Bar" }, values);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Detach()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var target = new Class1();
            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Inactive_Values_Should_Not_Be_Made_Active_During_Style_Detach_2()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Foo"),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "Bar"),
                        },
                    }
                }
            };

            var target = new Class1 { Classes = { "foo" } };
            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Foo", "foodefault" }, values);
        }

        [Fact]
        public void Inactive_Bindings_Should_Not_Be_Made_Active_During_Style_Detach()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Foo")),
                        },
                    },

                    new Style(x => x.OfType<Class1>())
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, new Binding("Bar")),
                        },
                    }
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

            root.Child = target;

            var values = new List<string>();
            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            root.Child = null;

            Assert.Equal(new[] { "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Template_In_Non_Matching_Style_Is_Not_Built()
        {
            var instantiationCount = 0;
            var template = new FuncTemplate<Class1>(() =>
            {
                ++instantiationCount;
                return new Class1();
            });

            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                },

                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                }
            };

            var target = new Class1();
            styles.TryAttach(target, null);

            Assert.NotNull(target.Child);
            Assert.Equal(1, instantiationCount);
        }

        [Fact]
        public void Template_In_Inactive_Style_Is_Not_Built()
        {
            var instantiationCount = 0;
            var template = new FuncTemplate<Class1>(() =>
            {
                ++instantiationCount;
                return new Class1();
            });

            Styles styles = new Styles
            {
                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                },

                new Style(x => x.OfType<Class1>())
                {
                    Setters =
                    {
                        new Setter(Class1.ChildProperty, template),
                    },
                }
            };

            var target = new Class1();
            target.GetValueStore().BeginStyling();
            styles.TryAttach(target, null);
            target.GetValueStore().EndStyling();

            Assert.NotNull(target.Child);
            Assert.Equal(1, instantiationCount);
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

            StyleHelpers.TryAttach(style, border);

            Assert.Equal(new Thickness(4), border.BorderThickness);
            root.Child = null;
            Assert.Equal(new Thickness(0), border.BorderThickness);
        }

        [Fact]
        public void Removing_Style_Should_Detach_From_Control()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                { 
                    new Style(x => x.OfType<Border>())
                    {
                        Setters =
                        {
                            new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                        }
                    }
                },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);

            root.Styles.RemoveAt(0);
            Assert.Equal(new Thickness(0), border.BorderThickness);
        }

        [Fact]
        public void Adding_Style_Should_Attach_To_Control()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Border>())
                    {
                        Setters =
                        {
                            new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                        }
                    }
                },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);

            root.Styles.Add(new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                }
            });

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(6), border.BorderThickness);
        }

        [Fact]
        public void Removing_Style_With_Nested_Style_Should_Detach_From_Control()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                {
                    new Styles
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        }
                    }
                },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);

            root.Styles.RemoveAt(0);
            Assert.Equal(new Thickness(0), border.BorderThickness);
        }

        [Fact]
        public void Adding_Nested_Style_Should_Attach_To_Control()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                {
                    new Styles
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        }
                    }
                },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);

            ((Styles)root.Styles[0]).Add(new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                }
            });

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(6), border.BorderThickness);
        }

        [Fact]
        public void Removing_Nested_Style_Should_Detach_From_Control()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                {
                    new Styles
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        },
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(6)),
                            }
                        },
                    }
                },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(6), border.BorderThickness);

            ((Styles)root.Styles[0]).RemoveAt(1);

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);
        }

        [Fact]
        public void Adding_Style_With_No_Setters_Or_Animations_Should_Not_Invalidate_Styles()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Styles =
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.BorderThicknessProperty, new Thickness(4)),
                            }
                        }
                    },
                Child = border,
            };

            root.Measure(Size.Infinity);
            Assert.Equal(new Thickness(4), border.BorderThickness);

            root.Styles.Add(new Style(x => x.OfType<Border>()));

            Assert.Equal(new Thickness(4), border.BorderThickness);
        }

        [Fact]
        public void Invalidating_Styles_Should_Detach_Activator()
        {
            Style style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target = new Class1();

            StyleHelpers.TryAttach(style, target);

            Assert.Equal(1, target.Classes.ListenerCount);

            target.InvalidateStyles(recurse: false);

            Assert.Equal(0, target.Classes.ListenerCount);
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources()
        {
            var host = new Mock<IResourceHost>();
            var target = new Style();
            ((IResourceProvider)target).AddOwner(host.Object);

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources_2()
        {
            var host = new Mock<IResourceHost>();
            var target = new Style();

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            host.Invocations.Clear();
            ((IResourceProvider)target).AddOwner(host.Object);
            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Nested_Style_Can_Be_Added()
        {
            var parent = new Style(x => x.OfType<Class1>());
            var nested = new Style(x => x.Nesting().Class("foo"));

            parent.Children.Add(nested);

            Assert.Same(parent, nested.Parent);
        }

        [Fact]
        public void Nested_Or_Style_Can_Be_Added()
        {
            var parent = new Style(x => x.OfType<Class1>());
            var nested = new Style(x => Selectors.Or(
                x.Nesting().Class("foo"),
                x.Nesting().Class("bar")));

            parent.Children.Add(nested);

            Assert.Same(parent, nested.Parent);
        }

        [Fact]
        public void Nested_Style_Without_Selector_Throws()
        {
            var parent = new Style(x => x.OfType<Class1>());
            var nested = new Style();

            Assert.Throws<InvalidOperationException>(() => parent.Children.Add(nested));
        }

        [Fact(Skip = "TODO")]
        public void Nested_Style_Without_Nesting_Operator_Throws()
        {
            var parent = new Style(x => x.OfType<Class1>());
            var nested = new Style(x => x.Class("foo"));

            Assert.Throws<InvalidOperationException>(() => parent.Children.Add(nested));
        }

        [Fact]
        public void Animations_Should_Be_Activated()
        {
            Style style = new Style(x => x.OfType<Class1>())
            {
                Animations =
                {
                    new Avalonia.Animation.Animation
                    {
                        Duration = TimeSpan.FromSeconds(1),
                        Children =
                        {
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter { Property = Class1.DoubleProperty, Value = 5.0 }
                                },
                            },
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter { Property = Class1.DoubleProperty, Value = 10.0 }
                                },
                                Cue = new Cue(1d)
                            }
                        },
                    }
                }
            };

            var clock = new TestClock();
            var target = new Class1 { Clock = clock };

            StyleHelpers.TryAttach(style, target);

            Assert.Equal(0.0, target.Double);

            clock.Step(TimeSpan.Zero);
            Assert.Equal(5.0, target.Double);

            clock.Step(TimeSpan.FromSeconds(0.5));
            Assert.Equal(7.5, target.Double);
        }

        [Fact]
        public void Animations_With_Trigger_Should_Be_Activated_And_Deactivated()
        {
            Style style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Animations =
                {
                    new Avalonia.Animation.Animation
                    {
                        Duration = TimeSpan.FromSeconds(1),
                        Children =
                        {
                            new KeyFrame
                            {
                                Setters = 
                                { 
                                    new Setter { Property = Class1.DoubleProperty, Value = 5.0 } 
                                },
                            },
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter { Property = Class1.DoubleProperty, Value = 10.0 }
                                },
                                Cue = new Cue(1d)
                            }
                        },
                    }
                }
            };

            var clock = new TestClock();
            var target = new Class1 { Clock = clock };

            StyleHelpers.TryAttach(style, target);

            Assert.Equal(0.0, target.Double);

            target.Classes.Add("foo");
            clock.Step(TimeSpan.Zero);
            Assert.Equal(5.0, target.Double);

            clock.Step(TimeSpan.FromSeconds(0.5));
            Assert.Equal(7.5, target.Double);

            target.Classes.Remove("foo");
            Assert.Equal(0.0, target.Double);
        }

        [Fact]
        public void Animations_With_Activator_Trigger_Should_Be_Activated_And_Deactivated()
        {
            var clock = new TestClock();
            var border = new Border();

            var root = new TestRoot
            {
                Clock = clock,
                Styles =
                {
                    new Style(x => x.OfType<Border>().Not(default(Selector).Class("foo")))
                    {
                        Setters =
                        {
                            new Setter(Border.BackgroundProperty, Brushes.Yellow),
                        },
                        Animations =
                        {
                            new Avalonia.Animation.Animation
                            {
                                Duration = TimeSpan.FromSeconds(1.0),
                                Children =
                                {
                                    new KeyFrame
                                    {
                                        Setters =
                                        {
                                            new Setter(Border.BackgroundProperty, Brushes.Green)
                                        },
                                        Cue = new Cue(0.0)
                                    },
                                    new KeyFrame
                                    {
                                        Setters =
                                        {
                                            new Setter(Border.BackgroundProperty, Brushes.Green)
                                        },
                                        Cue = new Cue(1.0)
                                    }
                                }
                            }
                        }
                    },
                    new Style(x => x.OfType<Border>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Border.BackgroundProperty, Brushes.Blue),
                        }
                    }
                },
                Child = border
            };

            root.Measure(Size.Infinity);

            Assert.Equal(Brushes.Yellow, border.Background);

            clock.Step(TimeSpan.FromSeconds(0.5));
            Assert.Equal(Brushes.Green, border.Background);

            border.Classes.Add("foo");
            Assert.Equal(Brushes.Blue, border.Background);
        }

        [Fact]
        public void Should_Not_Share_Instance_When_Or_Selector_Is_Present()
        {
            // Issue #13910
            Style style = new Style(x => Selectors.Or(x.OfType<Class1>(), x.OfType<Class2>().Class("bar")))
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "Foo"),
                },
            };

            var target1 = new Class1 { Classes = { "foo" } };
            var target2 = new Class2();

            StyleHelpers.TryAttach(style, target1);
            StyleHelpers.TryAttach(style, target2);

            Assert.Equal("Foo", target1.Foo);
            Assert.Equal("foodefault", target2.Foo);
        }

        private class Class1 : Control
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");

            public static readonly StyledProperty<Class1> ChildProperty =
                AvaloniaProperty.Register<Class1, Class1>(nameof(Child));

            public static readonly StyledProperty<double> DoubleProperty =
                AvaloniaProperty.Register<Class1, double>(nameof(Double));

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }

            public Class1 Child
            {
                get => GetValue(ChildProperty);
                set => SetValue(ChildProperty, value);
            }

            public double Double
            {
                get => GetValue(DoubleProperty);
                set => SetValue(DoubleProperty, value);
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                throw new NotImplementedException();
            }
        }

        private class Class2 : Control
        {
            public static readonly StyledProperty<string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>();

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }

            protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
            {
                base.OnPropertyChanged(change);
            }
        }
    }
}
