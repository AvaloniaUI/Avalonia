using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class InputElement_Focus
    {
        [Fact]
        public void Focus_Should_Set_FocusManager_Current()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();

                Assert.Same(target, root.FocusManager.GetFocusedElement());
            }
        }
        
        [Fact]
        public void Invisible_Controls_Should_Not_Receive_Focus()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button() { IsVisible = false}
                };
                
                Assert.Null(root.FocusManager.GetFocusedElement());

                target.Focus();
                
                Assert.False(target.IsFocused);
                Assert.False(target.IsKeyboardFocusWithin);

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }
        
        [Fact]
        public void Effectively_Invisible_Controls_Should_Not_Receive_Focus()
        {
            var target = new Button();
            Panel container;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        IsVisible = false,
                        Children = { target }
                    }
                };
                
                Assert.Null(root.FocusManager.GetFocusedElement());

                target.Focus();
                
                Assert.False(target.IsFocused);
                Assert.False(target.IsKeyboardFocusWithin);

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Trying_To_Focus_Invisible_Control_Should_Not_Change_Focus()
        {
            Button first;
            Button second;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = new StackPanel
                    { 
                        Children =
                        {
                            (first = new Button()),
                            (second = new Button() { IsVisible = false}),
                        }
                    }
                };

                first.Focus();

                Assert.Same(first, root.FocusManager.GetFocusedElement());

                second.Focus();

                Assert.Same(first, root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Disabled_Controls_Should_Not_Receive_Focus()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button() { IsEnabled = false }
                };

                Assert.Null(root.FocusManager.GetFocusedElement());

                target.Focus();

                Assert.False(target.IsFocused);
                Assert.False(target.IsKeyboardFocusWithin);

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Effectively_Disabled_Controls_Should_Not_Receive_Focus()
        {
            var target = new Button();
            Panel container;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        IsEnabled = false,
                        Children = { target }
                    }
                };

                Assert.Null(root.FocusManager.GetFocusedElement());

                target.Focus();

                Assert.False(target.IsFocused);
                Assert.False(target.IsKeyboardFocusWithin);

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Should_Not_Get_Restored_To_Enabled_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var sp = new StackPanel();
                Button target = new Button();
                Button target1 = new Button();
                target.Click += (s, e) => target.IsEnabled = false;
                target1.Click += (s, e) => target.IsEnabled = true;
                sp.Children.Add(target);
                sp.Children.Add(target1);
                var root = new TestRoot
                {
                    Child = sp
                };

                target.Focus();
                target.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));
                Assert.False(target.IsEnabled);
                Assert.False(target.IsFocused);
                target1.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));
                Assert.True(target.IsEnabled);
                Assert.False(target.IsFocused);
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Hidden()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                target.IsVisible = false;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact(Skip = "Need to implement IsEffectivelyVisible change notifications.")]
        public void Focus_Should_Be_Cleared_When_Control_Is_Effectively_Hidden()
        {
            Border container;
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = container = new Border
                    {
                        Child = target = new Button(),
                    }
                };

                target.Focus();
                container.IsVisible = false;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Disabled()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                target.IsEnabled = false;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Effectively_Disabled()
        {
            Border container;
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = container = new Border
                    {
                        Child = target = new Button(),
                    }
                };

                target.Focus();
                container.IsEnabled = false;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Removed_From_VisualTree()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                root.Child = null;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Pseudoclass_Should_Be_Applied_On_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();


                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus"));

                target2.Focus(NavigationMethod.Tab);
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus"));
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus"));
            }
        }

        [Fact]
        public void Control_FocusVsisible_Pseudoclass_Should_Be_Applied_On_Tab_And_DirectionalFocus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-visible"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus-visible"));

                target2.Focus(NavigationMethod.Tab);
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-visible"));
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus-visible"));

                target1.Focus(NavigationMethod.Directional);
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-visible"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus-visible"));
            }
        }
        
        [Fact]
        public void Control_FocusWithin_PseudoClass_Should_Be_Applied()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-within"));
                Assert.True(target1.IsKeyboardFocusWithin);
                Assert.True(root.Child.Classes.Contains(":focus-within"));
                Assert.True(root.Child.IsKeyboardFocusWithin);
                Assert.True(root.Classes.Contains(":focus-within"));
                Assert.True(root.IsKeyboardFocusWithin);
            }
        }
        
        [Fact]
        public void Control_FocusWithin_PseudoClass_Should_Be_Applied_and_Removed()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                var panel1 = new Panel { Children = { target1 } };
                var panel2 = new Panel { Children = { target2 } };
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            panel1,
                            panel2
                        }
                    }
                };
                
                target1.ApplyTemplate();
                target2.ApplyTemplate();

                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-within"));
                Assert.True(target1.IsKeyboardFocusWithin);
                Assert.True(panel1.Classes.Contains(":focus-within"));
                Assert.True(panel1.IsKeyboardFocusWithin);
                Assert.True(root.Child.Classes.Contains(":focus-within"));
                Assert.True(root.Child.IsKeyboardFocusWithin);
                Assert.True(root.Classes.Contains(":focus-within"));
                Assert.True(root.IsKeyboardFocusWithin);
                
                target2.Focus();
                
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-within"));
                Assert.False(target1.IsKeyboardFocusWithin);
                Assert.False(panel1.Classes.Contains(":focus-within"));
                Assert.False(panel1.IsKeyboardFocusWithin);
                Assert.True(root.Child.Classes.Contains(":focus-within"));
                Assert.True(root.Child.IsKeyboardFocusWithin);
                Assert.True(root.Classes.Contains(":focus-within"));
                Assert.True(root.IsKeyboardFocusWithin);
                
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus-within"));
                Assert.True(target2.IsKeyboardFocusWithin);
                Assert.True(panel2.Classes.Contains(":focus-within"));
                Assert.True(panel2.IsKeyboardFocusWithin);
            }
        }
        
        [Fact]
        public void Control_FocusWithin_Pseudoclass_Should_Be_Removed_When_Removed_From_Tree()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-within"));
                Assert.True(target1.IsKeyboardFocusWithin);
                Assert.True(root.Child.Classes.Contains(":focus-within"));
                Assert.True(root.Child.IsKeyboardFocusWithin);
                Assert.True(root.Classes.Contains(":focus-within"));
                Assert.True(root.IsKeyboardFocusWithin);

                Assert.Equal(KeyboardDevice.Instance.FocusedElement, target1);
                
                root.Child = null;
                
                Assert.Null(KeyboardDevice.Instance.FocusedElement);
                
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-within"));
                Assert.False(target1.IsKeyboardFocusWithin);
                Assert.False(root.Classes.Contains(":focus-within"));
                Assert.False(root.IsKeyboardFocusWithin);
            }
        }
        
        [Fact]
        public void Control_FocusWithin_Pseudoclass_Should_Be_Removed_Focus_Moves_To_Different_Root()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator { Focusable = true };
                var target2 = new Decorator { Focusable = true };
                
                var root1 = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                        }
                    }
                };
                
                var root2 = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target2,
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                target1.Focus();
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-within"));
                Assert.True(target1.IsKeyboardFocusWithin);
                Assert.True(root1.Child.Classes.Contains(":focus-within"));
                Assert.True(root1.Child.IsKeyboardFocusWithin);
                Assert.True(root1.Classes.Contains(":focus-within"));
                Assert.True(root1.IsKeyboardFocusWithin);

                Assert.Equal(KeyboardDevice.Instance.FocusedElement, target1);
                
                target2.Focus();
                
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-within"));
                Assert.False(target1.IsKeyboardFocusWithin);
                Assert.False(root1.Child.Classes.Contains(":focus-within"));
                Assert.False(root1.Child.IsKeyboardFocusWithin);
                Assert.False(root1.Classes.Contains(":focus-within"));
                Assert.False(root1.IsKeyboardFocusWithin);
                
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus-within"));
                Assert.True(target2.IsKeyboardFocusWithin);
                Assert.True(root2.Child.Classes.Contains(":focus-within"));
                Assert.True(root2.Child.IsKeyboardFocusWithin);
                Assert.True(root2.Classes.Contains(":focus-within"));
                Assert.True(root2.IsKeyboardFocusWithin);
            }
        }

        [Fact]
        public void Can_Clear_Focus()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                root.FocusManager.ClearFocus();

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Removing_Focused_Element_Inside_Focus_Scope_Activates_Root_Focus_Scope()
        {
            // Issue #13325
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button innerButton, intermediateButton, outerButton;
            TestFocusScope innerScope;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        // Intermediate focus scope to make sure that the root focus scope gets
                        // activated, not this one.
                        new TestFocusScope
                        {
                            Children =
                            {
                                (innerScope = new TestFocusScope
                                {
                                    Children =
                                    {
                                        (innerButton = new Button()),
                                    }
                                }),
                                (intermediateButton = new Button()),
                            }
                        },
                        (outerButton = new Button()),
                    }
                }
            };

            // Focus a control in each scope, ending with the innermost one.
            outerButton.Focus();
            intermediateButton.Focus();
            innerButton.Focus();

            // Remove the focused control from the tree.
            ((Panel)innerButton.Parent).Children.Remove(innerButton);

            var focusManager = Assert.IsType<FocusManager>(root.FocusManager);
            Assert.Same(outerButton, focusManager.GetFocusedElement());
            Assert.Null(focusManager.GetFocusedElement(innerScope));
        }

        [Fact]
        public void Removing_Focus_Scope_Activates_Root_Focus_Scope()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button innerButton, outerButton;
            TestFocusScope innerScope;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (innerScope = new TestFocusScope
                        {
                            Children =
                            {
                                (innerButton = new Button()),
                            }
                        }),
                        (outerButton = new Button()),
                    }
                }
            };

            // Focus a control in the top-level and inner focus scopes.
            outerButton.Focus();
            innerButton.Focus();

            // Remove the inner focus scope.
            ((Panel)innerScope.Parent).Children.Remove(innerScope);

            var focusManager = Assert.IsType<FocusManager>(root.FocusManager);
            Assert.Same(outerButton, focusManager.GetFocusedElement());
        }

        [Fact]
        public void Switching_Focus_Scope_Changes_Focus()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            Button innerButton, outerButton;
            TestFocusScope innerScope;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (innerScope = new TestFocusScope
                        {
                            Children =
                            {
                                (innerButton = new Button()),
                            }
                        }),
                        (outerButton = new Button()),
                    }
                }
            };

            // Focus a control in the top-level and inner focus scopes.
            outerButton.Focus();
            innerButton.Focus();

            var focusManager = Assert.IsType<FocusManager>(root.FocusManager);
            Assert.Same(innerButton, focusManager.GetFocusedElement());

            focusManager.SetFocusScope(root);
            Assert.Same(outerButton, focusManager.GetFocusedElement());

            focusManager.SetFocusScope(innerScope);
            Assert.Same(innerButton, focusManager.GetFocusedElement());
        }

        private class TestFocusScope : Panel, IFocusScope
        {
        }
    }
}
