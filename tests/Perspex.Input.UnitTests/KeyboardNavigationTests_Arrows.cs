// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Xunit;

namespace Perspex.Input.UnitTests
{
    using Controls = Controls.Controls;

    public class KeyboardNavigationTests_Arrows
    {
        [Fact]
        public void Down_Continue_Returns_Down_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_First_Control_In_Down_Sibling_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_Down_Sibling()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    (next = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_First_Control_In_Down_Uncle_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (container = new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children = new Controls
                                {
                                    new Button { Name = "Button1" },
                                    new Button { Name = "Button2" },
                                    (current = new Button { Name = "Button3" }),
                                }
                            }),
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_Child_Of_Top_Level()
        {
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children = new Controls
                {
                    (next = new Button { Name = "Button1" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(top, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Wraps()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children = new Controls
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            (container = new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children = new Controls
                                {
                                    (next = new Button { Name = "Button1" }),
                                    new Button { Name = "Button2" },
                                    new Button { Name = "Button3" },
                                }
                            }),
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Cycle_Returns_Down_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Cycle_Wraps_To_First()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Contained_Returns_Down_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Contained_Stops_At_End()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Null(result);
        }

        [Fact]
        public void Down_None_Does_Nothing()
        {
            StackPanel container;
            Button current;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.None,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Down);

            Assert.Null(result);
        }

        [Fact]
        public void Up_Continue_Returns_Up_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Control_In_Up_Sibling_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Child_Of_Sibling()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    (current = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Control_In_Up_Nephew_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (container = new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children = new Controls
                                {
                                    new Button { Name = "Button1" },
                                    new Button { Name = "Button2" },
                                    (next = new Button { Name = "Button3" }),
                                }
                            }),
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Wraps()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (container = new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children = new Controls
                                {
                                    (current = new Button { Name = "Button1" }),
                                    new Button { Name = "Button2" },
                                    new Button { Name = "Button3" },
                                }
                            }),
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (next = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Parent()
        {
            Button current;

            var top = new Decorator
            {
                Focusable = true,
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Child = current = new Button
                {
                    Name = "Button",
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(top, result);
        }

        [Fact]
        public void Up_Cycle_Returns_Up_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Cycle_Wraps_To_Last()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Contained_Returns_Up_Control_In_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Contained_Stops_At_Beginning()
        {
            StackPanel container;
            Button current;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Null(result);
        }

        [Fact]
        public void Up_Contained_Doesnt_Return_Child_Control()
        {
            Decorator current;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                Children = new Controls
                {
                    (current = new Decorator
                    {
                        Focusable = true,
                        Child = new Button(),
                    })
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Up);

            Assert.Null(result);
        }
    }
}
