// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigationTests_Tab.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.UnitTests
{
    using Perspex.Controls;
    using Xunit;

    public class KeyboardNavigationTests_Tab
    {
        [Fact]
        public void Next_Continue_Returns_Next_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_First_Control_In_Next_Sibling_Container()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }
        [Fact]
        public void Next_Continue_Doesnt_Enter_Panel_With_TabNavigation_None()
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
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children = new Controls
                        {
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
                    }
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_Next_Sibling()
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_First_Control_In_Next_Uncle_Container()
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
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Returns_Child_Of_Top_Level()
        {
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (next = new Button { Name = "Button1" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNext(top, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Continue_Wraps()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Cycle_Returns_Next_Control_In_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Cycle_Wraps_To_First()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Contained_Returns_Next_Control_In_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_Contained_Stops_At_End()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Null(result);
        }

        [Fact]
        public void Next_Once_Moves_To_Next_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

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
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Next_None_Moves_To_Next_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

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
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            (current = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Next);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Previous_Control_In_Container()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Control_In_Previous_Sibling_Container()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Child_Of_Sibling()
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Returns_Last_Control_In_Previous_Nephew_Container()
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
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Continue_Wraps()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            new Button { Name = "Button5" },
                            (next = new Button { Name = "Button6" }),
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(top, result);
        }

        [Fact]
        public void Previous_Cycle_Returns_Previous_Control_In_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Cycle_Wraps_To_Last()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Returns_Previous_Control_In_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            (current = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Stops_At_Beginning()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
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

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Null(result);
        }

        [Fact]
        public void Previous_Once_Moves_To_Previous_Container()
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
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (next = new Button { Name = "Button3" }),
                        }
                    }),
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children = new Controls
                        {
                            new Button { Name = "Button4" },
                            (current = new Button { Name = "Button5" }),
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

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
                Children = new Controls
                {
                    (container = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children = new Controls
                        {
                            new Button { Name = "Button1" },
                            (next = new Button { Name = "Button2" }),
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            KeyboardNavigation.SetTabOnceActiveElement(container, next);

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Once_Moves_To_First_Element()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children = new Controls
                        {
                            (next = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                            new Button { Name = "Button3" },
                        }
                    }),
                    new StackPanel
                    {
                        Children = new Controls
                        {
                            (current = new Button { Name = "Button4" }),
                            new Button { Name = "Button5" },
                            new Button { Name = "Button6" },
                        }
                    },
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Equal(next, result);
        }

        [Fact]
        public void Previous_Contained_Doesnt_Select_Child_Control()
        {
            Decorator current;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                Children = new Controls
                {
                    (current = new Decorator
                    {
                        Focusable = true,
                        Child = new Button(),
                    })
                }
            };

            var result = KeyboardNavigationHandler.GetNext(current, FocusNavigationDirection.Previous);

            Assert.Null(result);
        }
    }
}
