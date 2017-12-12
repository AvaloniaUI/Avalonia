// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class KeyboardNavigationTests_Arrows
    {
        [Fact]
        public void Down_Continue_Returns_Down_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_First_Control_In_Down_Sibling_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_Down_Sibling()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    (next = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_First_Control_In_Down_Uncle_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children =
                                {
                                    new Button { Name = "Button1" },
                                    new Button { Name = "Button2" },
                                    (current = new Button { Name = "Button3" }),
                                }
                            },
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Returns_Child_Of_Top_Level()
        {
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children =
                {
                    (next = new Button { Name = "Button1" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(top, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Continue_Wraps()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children =
                                {
                                    (next = new Button { Name = "Button1" }),
                                    new Button { Name = "Button2" },
                                    new Button { Name = "Button3" },
                                }
                            },
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Cycle_Returns_Down_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Cycle_Wraps_To_First()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Contained_Returns_Down_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Down_Contained_Stops_At_End()
        {
            Button current;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Null(result);
        }

        [Fact]
        public void Down_None_Does_Nothing()
        {
            Button current;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.None,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down);

            Assert.Null(result);
        }

        [Fact]
        public void Up_Continue_Returns_Up_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Control_In_Up_Sibling_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Child_Of_Sibling()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    (current = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Returns_Last_Control_In_Up_Nephew_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children =
                                {
                                    new Button { Name = "Button1" },
                                    new Button { Name = "Button2" },
                                    (next = new Button { Name = "Button3" }),
                                }
                            },
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Continue_Wraps()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new StackPanel
                            {
                                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                                Children =
                                {
                                    (current = new Button { Name = "Button1" }),
                                    new Button { Name = "Button2" },
                                    new Button { Name = "Button3" },
                                }
                            },
                        },
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (next = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(top, result);
        }

        [Fact]
        public void Up_Cycle_Returns_Up_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Cycle_Wraps_To_Last()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Contained_Returns_Up_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Up_Contained_Stops_At_Beginning()
        {
            Button current;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Null(result);
        }

        [Fact]
        public void Up_Contained_Doesnt_Return_Child_Control()
        {
            Decorator current;

            var top = new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Contained,
                Children =
                {
                    (current = new Decorator
                    {
                        Focusable = true,
                        Child = new Button(),
                    })
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Up);

            Assert.Null(result);
        }
    }
}
