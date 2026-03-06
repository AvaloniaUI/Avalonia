using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ComboBoxTests : ScopedTestBase
    {
        MouseTestHelper _helper = new MouseTestHelper();
        
        [Fact]
        public void Clicking_On_Control_Toggles_IsDropDownOpen()
        {
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo", "Bar" },
            };

            _helper.Down(target);
            _helper.Up(target);
            Assert.True(target.IsDropDownOpen);
            Assert.True(target.Classes.Contains(ComboBox.pcDropdownOpen));

            _helper.Down(target);
            _helper.Up(target);

            Assert.False(target.IsDropDownOpen);
            Assert.True(!target.Classes.Contains(ComboBox.pcDropdownOpen));
        }

        [Fact]
        public void Clicking_On_Control_PseudoClass()
        {
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo", "Bar" },
            };

            _helper.Down(target);
            Assert.True(target.Classes.Contains(ComboBox.pcPressed));
            _helper.Up(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));
            Assert.True(target.Classes.Contains(ComboBox.pcDropdownOpen));

            _helper.Down(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));
            _helper.Up(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));

            Assert.False(target.IsDropDownOpen);
            Assert.True(!target.Classes.Contains(ComboBox.pcDropdownOpen));
        }

        [Fact]
        public void WrapSelection_Should_Work()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items =
                    {
                        new ComboBoxItem() { Content = "bla" },
                        new ComboBoxItem() { Content = "dd" },
                        new ComboBoxItem() { Content = "sdf", IsEnabled = false }
                    },
                    Template = GetTemplate(),
                    WrapSelection = true
                };
                var root = new TestRoot(target);
                target.ApplyTemplate();
                target.Presenter!.ApplyTemplate();
                target.Focus();
                Assert.Equal(target.SelectedIndex, -1);
                Assert.True(target.IsFocused);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Up,
                });
                Assert.Equal(target.SelectedIndex, 1);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 0);
            }
        }

        [Fact]
        public void Focuses_Next_Item_On_Key_Down()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items =
                    {
                        new ComboBoxItem() { Content = "bla" },
                        new ComboBoxItem() { Content = "dd", IsEnabled = false },
                        new ComboBoxItem() { Content = "sdf" }
                    },
                    Template = GetTemplate()
                };
                var root = new TestRoot(target);
                target.ApplyTemplate();
                target.Presenter!.ApplyTemplate();
                target.Focus();
                Assert.Equal(target.SelectedIndex, -1);
                Assert.True(target.IsFocused);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 0);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 2);
            }
        }

        [Fact]
        public void SelectionBoxItem_Is_Rectangle_With_VisualBrush_When_Selection_Is_Control()
        {
            var target = new ComboBox
            {
                Items = { new Canvas() },
                SelectedIndex = 0,
            };
            var root = new TestRoot(target);

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);

            var brush = rectangle.Fill as VisualBrush;
            Assert.NotNull(brush);
            Assert.Same(target.Items[0], brush.Visual);
        }

        [Fact]
        public void SelectionBoxItem_Rectangle_Is_Removed_From_Logical_Tree()
        {
            var target = new ComboBox
            {
                Items = { new Canvas() },
                SelectedIndex = 0,
                Template = GetTemplate(),
            };

            var root = new TestRoot { Child = target };
            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);
            Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            Assert.True(((ILogical)rectangle).IsAttachedToLogicalTree);

            rectangle.DetachedFromLogicalTree += (s, e) => { };

            root.Child = null;

            Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            Assert.False(((ILogical)rectangle).IsAttachedToLogicalTree);
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ComboBox>((parent, scope) =>
            {
                return new Panel
                {
                    Name = "container",
                    Children =
                    {
                        new ContentControl
                        {
                            [!ContentControl.ContentProperty] = parent[!ComboBox.SelectionBoxItemProperty],
                        },
                        new Popup
                        {
                            Name = "PART_Popup",
                            Child = new ScrollViewer
                            {
                                Name = "PART_ScrollViewer",
                                Content = new ItemsPresenter
                                {
                                    Name = "PART_ItemsPresenter",
                                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
                                }.RegisterInNameScope(scope)
                            }.RegisterInNameScope(scope)
                        }.RegisterInNameScope(scope),
                        new TextBox
                        {
                            Name = "PART_EditableTextBox"
                        }.RegisterInNameScope(scope)
                    }
                };
            });
        }

        [Fact]
        public void Detaching_Closed_ComboBox_Keeps_Current_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items = { new Canvas() },
                    SelectedIndex = 0,
                    Template = GetTemplate(),
                };

                var other = new Control { Focusable = true };

                StackPanel panel;

                var root = new TestRoot { Child = panel = new StackPanel { Children = { target, other } } };

                target.ApplyTemplate();
                target.Presenter!.ApplyTemplate();

                other.Focus();

                Assert.True(other.IsFocused);

                panel.Children.Remove(target);

                Assert.True(other.IsFocused);
            }
        }

        [Theory]
        [InlineData(-1, 2, "c", "A item", "B item", "C item")]
        [InlineData(0, 1, "b", "A item", "B item", "C item")]
        [InlineData(2, 2, "x", "A item", "B item", "C item")]
        [InlineData(0, 34, "y", "0 item", "1 item", "2 item", "3 item", "4 item", "5 item", "6 item", "7 item", "8 item", "9 item", "A item", "B item", "C item", "D item", "E item", "F item", "G item", "H item", "I item", "J item", "K item", "L item", "M item", "N item", "O item", "P item", "Q item", "R item", "S item", "T item", "U item", "V item", "W item", "X item", "Y item", "Z item")]
        public void TextSearch_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            params string[] contents)
        {
            TestTextSearch(
                initialSelectedIndex,
                expectedSelectedIndex,
                searchTerm,
                _ => { },
                contents.Select(content => new ComboBoxItem { Content = content }));
        }

        [Theory]
        [InlineData(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
        [InlineData(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
        public void TextSearch_With_TextSearchText_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            string[] contents,
            string[] searchTexts)
        {
            Assert.Equal(contents.Length, searchTexts.Length);

            TestTextSearch(
                initialSelectedIndex,
                expectedSelectedIndex,
                searchTerm,
                _ => { },
                contents.Select((item, index) =>
                {
                    var comboBoxItem = new ComboBoxItem { Content = item };
                    TextSearch.SetText(comboBoxItem, searchTexts[index]);
                    return comboBoxItem;
                }));
        }

        [Theory]
        [InlineData(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
        [InlineData(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
        public void TextSearch_With_DisplayMemberBinding_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            string[] values,
            string[] displays)
        {
            Assert.Equal(values.Length, displays.Length);

            TestTextSearch(
                initialSelectedIndex,
                expectedSelectedIndex,
                searchTerm,
                comboBox => comboBox.DisplayMemberBinding = new Binding(nameof(Item.Display)),
                values.Select((value, index) => new Item(value, displays[index])));
        }

        [Theory]
        [InlineData(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
        [InlineData(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
        public void TextSearch_With_TextSearchBinding_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            string[] values,
            string[] displays)
        {
            Assert.Equal(values.Length, displays.Length);

            TestTextSearch(
                initialSelectedIndex,
                expectedSelectedIndex,
                searchTerm,
                comboBox => TextSearch.SetTextBinding(comboBox, new Binding(nameof(Item.Display))),
                values.Select((value, index) => new Item(value, displays[index])));
        }

        private static void TestTextSearch(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            Action<ComboBox> configureComboBox,
            IEnumerable<object> itemsSource)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new ComboBox
                {
                    Template = GetTemplate(),
                    ItemsSource = itemsSource.ToArray(),
                };

                configureComboBox(target);

                TestRoot root = new(target)
                {
                    ClientSize = new(500,500)
                };

                root.LayoutManager.ExecuteInitialLayoutPass();
                target.SelectedIndex = initialSelectedIndex;

                var args = new TextInputEventArgs
                {
                    Text = searchTerm,
                    RoutedEvent = InputElement.TextInputEvent
                };

                target.RaiseEvent(args);

                Assert.Equal(expectedSelectedIndex, target.SelectedIndex);
            }
        }

        [Fact]
        public void SelectedItem_Validation()
        {

            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new ComboBox
                {
                    Template = GetTemplate(),
                };

                target.ApplyTemplate();
                target.Presenter!.ApplyTemplate();
                
                var exception = new System.InvalidCastException("failed validation");
                var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                target.Bind(ComboBox.SelectedItemProperty, textObservable);

                Assert.True(DataValidationErrors.GetHasErrors(target));
                Assert.Equal([exception], DataValidationErrors.GetErrors(target));
            }
            
        }

        [Fact]
        public void Close_Window_On_Alt_F4_When_ComboBox_Is_Focus()
        {
            var inputManagerMock = new Moq.Mock<IInputManager>();
            var services = TestServices.StyledWindow.With(inputManager: inputManagerMock.Object);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.KeyDown += (s, e) =>
                 {
                     if (e.Handled == false 
                     && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) == true 
                     && e.Key == Key.F4 )
                     {
                         e.Handled = true;
                         window.Close();
                     }
                 };

                var count = 0;

                var target = new ComboBox
                {
                    Items = { new Canvas() },
                    SelectedIndex = 0,
                    Template = GetTemplate(),
                };

                window.Content = target;


                window.Closing +=
                    (sender, e) =>
                    {
                        count++;
                    };

                window.Show();

                target.Focus();

                _helper.Down(target);
                _helper.Up(target);
                Assert.True(target.IsDropDownOpen);

                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    KeyModifiers = KeyModifiers.Alt,
                    Key = Key.F4
                });


                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Should_Be_LeftToRight()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new ComboBox
            {
                FlowDirection = FlowDirection.RightToLeft,
                Items =
                {
                    new ComboBoxItem()
                    {
                        Content = new Control()
                    }
                },
                Template = GetTemplate()
            };

            var root = new TestRoot(target);
            target.ApplyTemplate();
            target.SelectedIndex = 0;

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);
            Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Updated_After_InvalidateMirrorTransform()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var parentContent = new Decorator()
            {
                Child = new Control()
            };
            var target = new ComboBox
            {
                Items = 
                {
                    new ComboBoxItem()
                    {
                        Content = parentContent.Child
                    }
                },
                Template = GetTemplate()
            };

            var root = new TestRoot(target);
            target.ApplyTemplate();
            target.SelectedIndex = 0;

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);
            Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);

            parentContent.FlowDirection = FlowDirection.RightToLeft;
            target.FlowDirection = FlowDirection.RightToLeft;
            
            Assert.Equal(FlowDirection.RightToLeft, rectangle.FlowDirection);
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Updated_After_OpenPopup()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parentContent = new Decorator()
                {
                    Child = new Control()
                };
                var target = new ComboBox
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    Items =
                    {
                        new ComboBoxItem()
                        {
                            Content = parentContent.Child,
                            Template = null // ugly hack, so we can "attach" same child to the two different trees
                        }
                    },
                    Template = GetTemplate()
                };

                var root = new TestRoot(target);
                target.ApplyTemplate();
                target.SelectedIndex = 0;

                var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
                Assert.NotNull(rectangle);
                Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);

                parentContent.FlowDirection = FlowDirection.RightToLeft;

                var popup = target.GetVisualDescendants().OfType<Popup>().First();
                popup.PlacementTarget = new Window();
                popup.Open();
                
                Assert.Equal(FlowDirection.RightToLeft, rectangle.FlowDirection);
            }
        }

        [Fact]
        public void SelectionBoxItemTemplate_Overrides_ItemTemplate()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            var target = new ComboBox
            {
                ItemsSource = new []{ "Foo" },
                SelectionBoxItemTemplate = selectionBoxItemTemplate,
                ItemTemplate = itemTemplate,
            };
            
            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
        }
        
        [Fact]
        public void SelectionBoxItemTemplate_Inherits_From_ItemTemplate_When_NotSet()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            var target = new ComboBox
            {
                ItemsSource = new []{ "Foo" },
                ItemTemplate = itemTemplate,
            };
            
            Assert.Equal(itemTemplate, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void SelectionBoxItemTemplate_Overrides_ItemTemplate_After_ItemTemplate_Changed()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo" },
                SelectionBoxItemTemplate = selectionBoxItemTemplate,
                ItemTemplate = itemTemplate,
            };

            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
            
            target.ItemTemplate = itemTemplate2;
            
            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void SelectionBoxItemTemplate_Inherits_From_ItemTemplate_When_ItemTemplate_Changed()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
            var target = new ComboBox { ItemsSource = new[] { "Foo" }, ItemTemplate = itemTemplate, };

            Assert.Equal(itemTemplate, target.SelectionBoxItemTemplate);

            target.ItemTemplate = itemTemplate2;
            target.SelectionBoxItemTemplate = null;

            Assert.Equal(itemTemplate2, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void DisplayMemberBinding_Is_Not_Applied_To_SelectionBoxItem_Without_Selection()
        {
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding(),
                ItemsSource = new[] { "foo", "bar" }
            };

            target.SelectedItem = null;
            Assert.Null(target.SelectionBoxItem);

            target.SelectedItem = "foo";
            Assert.NotNull(target.SelectionBoxItem);

            target.SelectedItem = null;
            Assert.Null(target.SelectionBoxItem);
        }

        private sealed record Item(string Value, string Display);

        [Fact]
        public void When_Editable_Input_Text_Matches_An_Item_It_Is_Selected()
        {
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding(),
                IsEditable = true,
                ItemsSource = new[] { "foo", "bar" }
            };

            target.SelectedItem = null;
            Assert.Null(target.SelectedItem);

            target.Text = "foo";
            Assert.NotNull(target.SelectedItem);
            Assert.Equal(target.SelectedItem, "foo");
        }

        [Fact]
        public void When_Editable_TextSearch_TextBinding_Is_Prioritised_Over_DisplayMember()
        {
            var items = new[]
            {
                new Item("Value 1", "Display 1"),
                new Item("Value 2", "Display 2")
            };
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding("Display"),
                IsEditable = true,
                ItemsSource = items
            };
            TextSearch.SetTextBinding(target, new Binding("Value"));

            target.SelectedItem = null;
            Assert.Null(target.SelectedItem);

            target.Text = "Value 1";
            Assert.NotNull(target.SelectedItem);
            Assert.Equal(target.SelectedItem, items[0]);
        }

        [Fact]
        public void When_Items_Source_Changes_It_Selects_An_Item_By_Text()
        {
            var items = new[]
            {
                new Item("Value 1", "Display 1"),
                new Item("Value 2", "Display 2")
            };
            var items2 = new[]
            {
                new Item("Value 1", "Display 3"),
                new Item("Value 2", "Display 4")
            };
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding("Display"),
                IsEditable = true,
                ItemsSource = items
            };
            TextSearch.SetTextBinding(target, new Binding("Value"));

            target.SelectedItem = null;
            Assert.Null(target.SelectedItem);

            target.Text = "Value 1";
            Assert.NotNull(target.SelectedItem);
            Assert.Equal(target.SelectedItem, items[0]);

            target.ItemsSource = items2;
            Assert.NotNull(target.SelectedItem);
            Assert.Equal(target.SelectedItem, items2[0]);
            Assert.Equal(target.Text, "Value 1");
        }

        private void RaiseTabKeyPress(Control target, bool withShift = false)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Tab,
                KeyModifiers = withShift ? KeyModifiers.Shift : KeyModifiers.None
            });

            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyUpEvent,
                Key = Key.Tab,
                KeyModifiers = withShift ? KeyModifiers.Shift : KeyModifiers.None
            });
        }

        [Fact]
        public void When_Tabbing_Out_With_Dropdown_Open_It_Closes()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);

            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo", "Bar" }
            };
            var nextControl = new ComboBox
            {
                ItemsSource = new[] { "Baz" }
            };

            var container = new StackPanel
            {
                Children =
                {
                    target,
                    nextControl
                }
            };
            var root = new TestRoot(container);
            var keyboardNavHandler = new KeyboardNavigationHandler();
            keyboardNavHandler.SetOwner(root);

            target.Focus();
            _helper.Down(target);
            _helper.Up(target);
            Assert.True(target.IsFocused);
            Assert.True(target.IsDropDownOpen);

            RaiseTabKeyPress(target);

            Assert.False(target.IsFocused);
            Assert.True(nextControl.IsFocused);
            Assert.False(target.IsDropDownOpen);
        }

        [Fact]
        public void When_Editable_And_Item_Selected_Via_Text_Then_Focus_Swaps_Via_Tab_Swapping_Back_Should_Focus_TextBox()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);

            var items = new[]
            {
                new Item("Value 1", "Display 1"),
                new Item("Value 2", "Display 2")
            };
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding("Display"),
                IsEditable = true,
                IsTabStop = false,
                ItemsSource = items,
                Template = GetTemplate()
            };
            TextSearch.SetTextBinding(target, new Binding("Value"));
            KeyboardNavigation.SetTabNavigation(target, KeyboardNavigationMode.Local);

            var previousControl = new ComboBox
            {
                ItemsSource = new[] { "Baz" }
            };

            var container = new StackPanel
            {
                Children =
                {
                    previousControl,
                    target
                }
            };
            var root = new TestRoot(container);
            var keyboardNavHandler = new KeyboardNavigationHandler();
            keyboardNavHandler.SetOwner(root);

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            var containerPanel = target.GetTemplateChildren().OfType<Panel>().FirstOrDefault(x => x.Name == "container");
            var editableTextBox = containerPanel?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "PART_EditableTextBox");
            var popup = containerPanel?.GetVisualDescendants().OfType<Popup>().FirstOrDefault(x => x.Name == "PART_Popup");
            var popupScrollViewer = popup?.Child as ScrollViewer;
            var scrollViewerItemsPresenter = popupScrollViewer?.Content as ItemsPresenter;
            var popupVirtualizingStackPanel = scrollViewerItemsPresenter?.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();

            Assert.NotNull(editableTextBox);
            Assert.NotNull(scrollViewerItemsPresenter);
            Assert.NotNull(popupVirtualizingStackPanel);

            //force the popup to render the ComboBoxItem(s) as they are what get set as "focused" if this test fails
            popupVirtualizingStackPanel.Measure(Size.Infinity);

            target.Focus();
            Assert.True(editableTextBox.IsFocused);

            target.Text = "Value 1";
            Assert.Same(target.SelectedItem, items[0]);
            var item1 = scrollViewerItemsPresenter.ContainerFromIndex(0);
            Assert.IsType<ComboBoxItem>(item1);

            RaiseTabKeyPress(target, withShift: true);

            Assert.False(target.IsFocused);
            Assert.True(previousControl.IsFocused);

            RaiseTabKeyPress(previousControl);

            var focused = root.FocusManager.GetFocusedElement();
            Assert.Same(editableTextBox, focused);
        }
    }
}
