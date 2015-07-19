// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigationTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.UnitTests
{
    using Perspex.Controls;
    using Xunit;

    public class KeyboardNavigationTests
    {
        [Fact]
        public void GetNextInTabOrder_Continue_Returns_Next_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Continue_Returns_First_Control_In_Next_Sibling_Container()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Continue_Returns_Next_Sibling()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Continue_Returns_First_Control_In_Next_Uncle_Container()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Continue_Returns_Child_Of_Top_Level()
        {
            Button next;

            var top = new StackPanel
            {
                Children = new Controls
                {
                    (next = new Button { Name = "Button1" }),
                }
            };

            var result = KeyboardNavigationHandler.GetNextInTabOrder(top);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Continue_Wraps()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Cycle_Returns_Next_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Cycle_Wraps_To_First()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Once_Moves_To_Next_Container()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Once_Moves_To_Active_Element()
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Never_Moves_To_Next_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Never,
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetNextInTabOrder_Never_Skips_Container()
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
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Never,
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

            var result = KeyboardNavigationHandler.GetNextInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Continue_Returns_Previous_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Continue_Returns_Last_Control_In_Previous_Sibling_Container()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Continue_Returns_Last_Child_Of_Sibling()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Continue_Returns_Last_Control_In_Previous_Nephew_Container()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Continue_Wraps()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Cycle_Returns_Previous_Control_In_Container()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Cycle_Wraps_To_Last()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Once_Moves_To_Previous_Container()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Once_Moves_To_Active_Element()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }

        [Fact]
        public void GetPreviousInTabOrder_Once_Moves_To_First_Element()
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

            var result = KeyboardNavigationHandler.GetPreviousInTabOrder(current);

            Assert.Equal(next, result);
        }
    }
}
