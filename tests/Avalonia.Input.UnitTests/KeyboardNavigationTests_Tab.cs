// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class KeyboardNavigationTests_Tab
    {
        [Fact]
        public void Next_Continue_Returns_Next_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_First_Control_In_Next_Sibling_Container()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Skips_Unfocusable_Siblings()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            new StackPanel
                            {
                                Children =
                                {
                                    (current = new Button { Name = "Button3" }),
                                }
                            },
                            new TextBlock { Name = "TextBlock" },
                            (next = new Button { Name = "Button4" }),
                        }
                    },
                    new Button { Name = "Button5" },
                    new Button { Name = "Button6" },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Doesnt_Enter_Panel_With_TabNavigation_None()
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
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children =
                        {
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
                    }
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_Next_Sibling()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    },
                    (next = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_First_Control_In_Next_Uncle_Container()
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
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_Child_Of_Top_Level()
        {
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    (next = new Button { Name = "Button1" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(top, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Wraps()
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
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Cycle_Returns_Next_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Cycle_Wraps_To_First()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Contained_Returns_Next_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Contained_Stops_At_End()
        {
            Button current;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            new Button { Name = "Button1" },
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Null(result);
        }

        [Fact]
        public void Next_Once_Moves_To_Next_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Once_Moves_To_Active_Element()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_None_Moves_To_Next_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_None_Skips_Container()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children =
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Previous_Control_In_Container()
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
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Control_In_Previous_Sibling_Container()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Child_Of_Sibling()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    (current = new Button { Name = "Button4" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Control_In_Previous_Nephew_Container()
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
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Wraps()
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
                        Children =
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (next = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Parent()
        {
            Button current;

            var top = new Decorator
            {
                Focusable = true,
                Child = current = new Button
                {
                    Name = "Button",
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(top, result);
        }

        [Fact]
        public void Previous_Cycle_Returns_Previous_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Cycle_Wraps_To_Last()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Returns_Previous_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Stops_At_Beginning()
        {
            Button current;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
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

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Null(result);
        }

        [Fact]
        public void Previous_Once_Moves_To_Previous_Container()
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
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    },
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            new Button { Name = "Button4" },
                            (current = new Button { Name = "Button5" }),
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Once_Moves_To_Active_Element()
        {
            StackPanel container;
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Once_Moves_To_First_Element()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Doesnt_Select_Child_Control()
        {
            Decorator current;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                Children =
                {
                    (current = new Decorator
                    {
                        Focusable = true,
                        Child = new Button(),
                    })
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

            Assert.Null(result);
        }
    }
}
