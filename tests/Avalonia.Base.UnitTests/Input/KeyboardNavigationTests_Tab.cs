using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyboardNavigationTests_Tab : ScopedTestBase
    {
        [Fact]
        public void Next_Continue_Returns_Next_Control_In_Container()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
        public void Next_Skips_Non_TabStop_Siblings()
        {
            Button current;
            Button next;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button1" },
                            new Button { Name = "Button2" },
                            (current = new Button { Name = "Button3" }),
                            new Button { Name="Button4", [KeyboardNavigation.IsTabStopProperty] = false }
                        }
                    },
                    (next = new Button { Name = "Button5" }),
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
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

        [Fact]
        public void Respects_TabIndex_Moving_Forwards()
        {
            Button start;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button1", TabIndex = 5 },
                            (start = new Button { Name = "Button2", TabIndex = 2 }),
                            new Button { Name = "Button3", TabIndex = 1 },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4", TabIndex = 3  },
                            new Button { Name = "Button5", TabIndex = 6  },
                            new Button { Name = "Button6", TabIndex = 4  },
                        }
                    },
                }
            };

            var result = new List<string?>();
            var current = (IInputElement)start;

            do
            {
                result.Add(((Control)current).Name);
                current = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);
            } while (current is object && current != start);

            Assert.Equal(new[]
            {
                "Button2", "Button4", "Button6", "Button1", "Button5", "Button3"
            }, result);
        }

        [Fact]
        public void Respects_TabIndex_Moving_Backwards()
        {
            Button start;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button1", TabIndex = 5 },
                            (start = new Button { Name = "Button2", TabIndex = 2 }),
                            new Button { Name = "Button3", TabIndex = 1 },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4", TabIndex = 3  },
                            new Button { Name = "Button5", TabIndex = 6  },
                            new Button { Name = "Button6", TabIndex = 4  },
                        }
                    },
                }
            };

            var result = new List<string?>();
            var current = (IInputElement)start;

            do
            {
                result.Add(((Control)current).Name);
                current = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);
            } while (current is not null && current != start);

            Assert.Equal(new[]
            {
                "Button2", "Button3", "Button5", "Button1", "Button6", "Button4"
            }, result);
        }

        [Fact]
        public void Cannot_Focus_Child_Of_Disabled_Control()
        {
            Button start;
            Button expected;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    (start = new Button { Name = "Button1" }),
                    new Border
                    {
                        IsEnabled = false,
                        Child = new Button { Name = "Button2" },
                    },
                    (expected = new Button { Name = "Button3" }),
                }
            };

            var current = (IInputElement)start;
            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

            Assert.Same(expected, result);
        }

        [Fact]
        public void Focuses_First_Child_From_No_Focus()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var button = new Button();
            var root = new TestRoot(button);
            var target = new KeyboardNavigationHandler();

            target.SetOwner(root);

            root.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Tab,
            });

            Assert.True(button.IsFocused);
        }

        [Fact]
        public void Next_Skip_Button_When_Command_CanExecute_Is_False()
        {
            Button current;
            Button expected;
            bool executed = false;

            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            (current = new Button { Name = "Button1" }),
                            new Button
                            {
                                Name = "Button2",
                                Command = new Utilities.DelegateCommand(()=>executed = true,
                                    _ => false),
                            },
                            (expected = new Button { Name = "Button3" }),
                        }
                    }
                }
            };

            var testRoot = new TestRoot(top);

            top.ApplyTemplate();

            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);

            var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next) as Button;

            Assert.Equal(expected.Name, result?.Name);
            Assert.False(executed);
        }

        [Fact]
        public void FocusManager_Contained_Stops_At_Both_Edges()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button first, middle, last;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                        Children =
                        {
                            (first = new Button { Name = "Button1" }),
                            (middle = new Button { Name = "Button2" }),
                            (last = new Button { Name = "Button3" }),
                        }
                    },
                    new Button { Name = "Button4" },
                }
            });

            Assert.Same(middle, FindNext(focusManager, first));
            Assert.Null(FindNext(focusManager, last));
            Assert.Null(FindPrevious(focusManager, first));
        }

        [Fact]
        public void FocusManager_Contained_Stops_Instead_Of_Wrapping_At_Root()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button first, last;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
                    Children =
                    {
                        (first = new Button { Name = "Button1" }),
                        (last = new Button { Name = "Button2" }),
                    }
                }
            };
            var focusManager = (FocusManager)root.FocusManager;

            Assert.Null(FindNext(focusManager, last));
            Assert.Null(FindPrevious(focusManager, first));
        }

        [Fact]
        public void FocusManager_Continue_Compares_TabIndex_Across_Panels()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button start;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button1", TabIndex = 5 },
                            (start = new Button { Name = "Button2", TabIndex = 2 }),
                            new Button { Name = "Button3", TabIndex = 1 },
                        }
                    },
                    new StackPanel
                    {
                        Children =
                        {
                            new Button { Name = "Button4", TabIndex = 3 },
                            new Button { Name = "Button5", TabIndex = 6 },
                            new Button { Name = "Button6", TabIndex = 4 },
                        }
                    },
                }
            });

            var forwards = new List<string?>();
            var backwards = new List<string?>();
            var current = (IInputElement?)start;

            do
            {
                forwards.Add(((Control)current!).Name);
                current = FindNext(focusManager, current);
            } while (current is not null && current != start);

            do
            {
                backwards.Add(((Control)current!).Name);
                current = FindPrevious(focusManager, current);
            } while (current is not null && current != start);

            Assert.Equal(new[] { "Button2", "Button4", "Button6", "Button1", "Button5", "Button3" }, forwards);
            Assert.Equal(new[] { "Button2", "Button3", "Button5", "Button1", "Button6", "Button4" }, backwards);
        }

        [Fact]
        public void FocusManager_Does_Not_Enter_Container_With_TabNavigation_None()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button before, after;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    (before = new Button { Name = "Button1" }),
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
                        Children = { new Button { Name = "Button2" } }
                    },
                    (after = new Button { Name = "Button3" }),
                }
            });

            Assert.Same(after, FindNext(focusManager, before));
            Assert.Same(before, FindPrevious(focusManager, after));
        }

        [Fact]
        public void FocusManager_Once_Enters_On_Active_Element()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            StackPanel once;
            Button before, active, after;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    (before = new Button { Name = "Button1" }),
                    (once = new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            new Button { Name = "Button2" },
                            (active = new Button { Name = "Button3" }),
                            new Button { Name = "Button4" },
                        }
                    }),
                    (after = new Button { Name = "Button5" }),
                }
            });

            KeyboardNavigation.SetTabOnceActiveElement(once, active);

            Assert.Same(active, FindNext(focusManager, before));
            Assert.Same(active, FindPrevious(focusManager, after));
            Assert.Same(after, FindNext(focusManager, active));
        }

        [Fact]
        public void FocusManager_Once_Enters_On_First_Element_Moving_Backwards_Without_Active_Element()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button first, after;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
                        Children =
                        {
                            (first = new Button { Name = "Button1" }),
                            new Button { Name = "Button2" },
                        }
                    },
                    (after = new Button { Name = "Button3" }),
                }
            });

            Assert.Same(first, FindPrevious(focusManager, after));
        }

        [Fact]
        public void FocusManager_Nested_Cycle_Wraps_Inside_Container()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button first, last;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    new Button { Name = "Before" },
                    new StackPanel
                    {
                        [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                        Children =
                        {
                            (first = new Button { Name = "Button1" }),
                            (last = new Button { Name = "Button2" }),
                        }
                    },
                    new Button { Name = "After" },
                }
            });

            Assert.Same(first, FindNext(focusManager, last));
            Assert.Same(last, FindPrevious(focusManager, first));
        }

        [Fact]
        public void FocusManager_Skips_Child_Of_Disabled_Control()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button before, after;
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    (before = new Button { Name = "Button1" }),
                    new Border { IsEnabled = false, Child = new Button { Name = "Button2" } },
                    (after = new Button { Name = "Button3" }),
                }
            });

            Assert.Same(after, FindNext(focusManager, before));
            Assert.Same(before, FindPrevious(focusManager, after));
        }

        [Fact]
        public void FocusManager_Leaves_Content_Of_Templated_Control_In_Tab_Order()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button content, after;
            var contentControl = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((parent, _) => new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                }),
                Content = content = new Button { Name = "Content" },
            };
            var focusManager = CreateCycleRoot(new StackPanel
            {
                Children =
                {
                    contentControl,
                    (after = new Button { Name = "After" }),
                }
            });

            contentControl.ApplyTemplate();
            contentControl.Presenter!.UpdateChild();

            Assert.Same(after, FindNext(focusManager, content));
            Assert.Same(content, FindPrevious(focusManager, after));
        }

        [Fact]
        public void Tab_Stops_At_Edge_Of_Continue_Root_While_FocusManager_Wraps()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button first, last;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (first = new Button { Name = "Button1" }),
                        (last = new Button { Name = "Button2" }),
                    }
                }
            };
            root[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Continue;
            var focusManager = (FocusManager)root.FocusManager;

            Assert.Null(KeyboardNavigationHandler.GetNext(last, NavigationDirection.Next));
            Assert.Null(KeyboardNavigationHandler.GetNext(first, NavigationDirection.Previous));
            Assert.Same(first, FindNext(focusManager, last));
            Assert.Same(last, FindPrevious(focusManager, first));
        }

        [Fact]
        public void Tab_Honors_GetNextTabStopOverride()
        {
            Button current, target;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    (target = new Button { Name = "Target" }),
                    new Button { Name = "Skipped" },
                }
            };
            top.Children.Insert(1, current = new TabStopOverrideButton { NextTabStop = target });

            Assert.Same(target, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next));
        }

        [Fact]
        public void Tab_Honors_GetFirstFocusableElementOverride_When_Entering_Container()
        {
            Button before, target;
            var container = new FirstFocusableOverridePanel
            {
                Children =
                {
                    new Button { Name = "Skipped" },
                    (target = new Button { Name = "Target" }),
                }
            };
            container.FirstFocusable = target;

            var top = new StackPanel
            {
                [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
                Children =
                {
                    (before = new Button { Name = "Before" }),
                    container,
                }
            };

            Assert.Same(target, KeyboardNavigationHandler.GetNext(before, NavigationDirection.Next));
        }

        private static FocusManager CreateCycleRoot(Control child)
        {
            var root = new TestRoot { Child = child };
            root[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle;
            return (FocusManager)root.FocusManager;
        }

        private static IInputElement? FindNext(FocusManager focusManager, IInputElement? focused) =>
            focusManager.FindNextElement(
                NavigationDirection.Next,
                new FindNextElementOptions { FocusedElement = focused });

        private static IInputElement? FindPrevious(FocusManager focusManager, IInputElement? focused) =>
            focusManager.FindNextElement(
                NavigationDirection.Previous,
                new FindNextElementOptions { FocusedElement = focused });

        private class TabStopOverrideButton : Button
        {
            public InputElement? NextTabStop { get; set; }

            protected internal override InputElement? GetNextTabStopOverride() => NextTabStop;
        }

        private class FirstFocusableOverridePanel : StackPanel
        {
            public InputElement? FirstFocusable { get; set; }

            protected internal override InputElement? GetFirstFocusableElementOverride() => FirstFocusable;
        }
    }
}
