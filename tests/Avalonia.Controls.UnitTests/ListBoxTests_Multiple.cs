using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxTests_Multiple
    {
        private MouseTestHelper _helper = new MouseTestHelper();

        [Fact]
        public void Shift_Selecting_From_No_Selection_Selects_From_Start()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Shift);

                Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
                Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
            }
        }


        [Fact]
        public void Ctrl_Selecting_Raises_SelectionChanged_Events()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

                SelectionChangedEventArgs receivedArgs = null;

                target.SelectionChanged += (_, args) => receivedArgs = args;

                void VerifyAdded(string selection)
                {
                    Assert.NotNull(receivedArgs);
                    Assert.Equal(new[] { selection }, receivedArgs.AddedItems);
                    Assert.Empty(receivedArgs.RemovedItems);
                }

                void VerifyRemoved(string selection)
                {
                    Assert.NotNull(receivedArgs);
                    Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                    Assert.Empty(receivedArgs.AddedItems);
                }

                _helper.Click(target.Presenter.Panel.Children[1]);

                VerifyAdded("Bar");

                receivedArgs = null;
                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Control);

                VerifyAdded("Baz");

                receivedArgs = null;
                _helper.Click(target.Presenter.Panel.Children[3], modifiers: KeyModifiers.Control);

                VerifyAdded("Qux");

                receivedArgs = null;
                _helper.Click(target.Presenter.Panel.Children[1], modifiers: KeyModifiers.Control);

                VerifyRemoved("Bar");
            }
        }

        [Fact]
        public void Ctrl_Selecting_SelectedItem_With_Multiple_Selection_Active_Sets_SelectedItem_To_Next_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[1]);
                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Control);
                _helper.Click(target.Presenter.Panel.Children[3], modifiers: KeyModifiers.Control);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal("Bar", target.SelectedItem);
                Assert.Equal(new[] { "Bar", "Baz", "Qux" }, target.SelectedItems);

                _helper.Click(target.Presenter.Panel.Children[1], modifiers: KeyModifiers.Control);

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal("Baz", target.SelectedItem);
                Assert.Equal(new[] { "Baz", "Qux" }, target.SelectedItems);
            }
        }

        [Fact]
        public void Ctrl_Selecting_Non_SelectedItem_With_Multiple_Selection_Active_Leaves_SelectedItem_The_Same()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[1]);
                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Control);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal("Bar", target.SelectedItem);

                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Control);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal("Bar", target.SelectedItem);
            }
        }

        [Fact]
        public void Should_Ctrl_Select_Correct_Item_When_Duplicate_Items_Are_Present()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[3]);
                _helper.Click(target.Presenter.Panel.Children[4], modifiers: KeyModifiers.Control);

                var panel = target.Presenter.Panel;

                Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
                Assert.Equal(new[] { 3, 4 }, SelectedContainers(target));
            }
        }

        [Fact]
        public void Should_Shift_Select_Correct_Item_When_Duplicates_Are_Present()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[3]);
                _helper.Click(target.Presenter.Panel.Children[5], modifiers: KeyModifiers.Shift);

                var panel = target.Presenter.Panel;

                Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
                Assert.Equal(new[] { 3, 4, 5 }, SelectedContainers(target));
            }
        }

        [Fact]
        public void Can_Shift_Select_All_Items_When_Duplicates_Are_Present()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[0]);
                _helper.Click(target.Presenter.Panel.Children[5], modifiers: KeyModifiers.Shift);

                var panel = target.Presenter.Panel;

                Assert.Equal(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.SelectedItems);
                Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, SelectedContainers(target));
            }
        }

        [Fact]
        public void Shift_Selecting_Raises_SelectionChanged_Events()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };
                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

                SelectionChangedEventArgs receivedArgs = null;

                target.SelectionChanged += (_, args) => receivedArgs = args;

                void VerifyAdded(params string[] selection)
                {
                    Assert.NotNull(receivedArgs);
                    Assert.Equal(selection, receivedArgs.AddedItems);
                    Assert.Empty(receivedArgs.RemovedItems);
                }

                void VerifyRemoved(string selection)
                {
                    Assert.NotNull(receivedArgs);
                    Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                    Assert.Empty(receivedArgs.AddedItems);
                }

                _helper.Click(target.Presenter.Panel.Children[1]);

                VerifyAdded("Bar");

                receivedArgs = null;
                _helper.Click(target.Presenter.Panel.Children[3], modifiers: KeyModifiers.Shift);

                VerifyAdded("Baz", "Qux");

                receivedArgs = null;
                _helper.Click(target.Presenter.Panel.Children[2], modifiers: KeyModifiers.Shift);

                VerifyRemoved("Qux");
            }
        }

        [Fact]
        public void Duplicate_Items_Are_Added_To_SelectedItems_In_Order()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[0]);

                Assert.Equal(new[] { "Foo" }, target.SelectedItems);

                _helper.Click(target.Presenter.Panel.Children[4], modifiers: KeyModifiers.Control);

                Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);

                _helper.Click(target.Presenter.Panel.Children[3], modifiers: KeyModifiers.Control);

                Assert.Equal(new[] { "Foo", "Bar", "Foo" }, target.SelectedItems);

                _helper.Click(target.Presenter.Panel.Children[1], modifiers: KeyModifiers.Control);

                Assert.Equal(new[] { "Foo", "Bar", "Foo", "Bar" }, target.SelectedItems);
            }
        }

        [Fact]
        public void Left_Click_On_SelectedItem_Should_Clear_Existing_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                target.SelectAll();

                Assert.Equal(3, target.SelectedItems.Count);

                _helper.Click(target.Presenter.Panel.Children[0]);

                Assert.Equal(1, target.SelectedItems.Count);
                Assert.Equal(new[] { "Foo", }, target.SelectedItems);
                Assert.Equal(new[] { 0 }, SelectedContainers(target));
            }
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                target.SelectAll();

                Assert.Equal(3, target.SelectedItems.Count);

                _helper.Click(target.Presenter.Panel.Children[0], MouseButton.Right);

                Assert.Equal(3, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
                _helper.Click(target.Presenter.Panel.Children[0]);
                _helper.Click(target.Presenter.Panel.Children[1], modifiers: KeyModifiers.Shift);

                Assert.Equal(2, target.SelectedItems.Count);

                _helper.Click(target.Presenter.Panel.Children[2], MouseButton.Right);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

                _helper.Click(target.Presenter.Panel.Children[0]);
                _helper.Click(target.Presenter.Panel.Children[2], MouseButton.Right, modifiers: KeyModifiers.Shift);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = new[] { "Foo", "Bar", "Baz" },
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectionMode = SelectionMode.Multiple,
                    Width = 100,
                    Height = 100,
                };

                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

                _helper.Click(target.Presenter.Panel.Children[0]);
                _helper.Click(target.Presenter.Panel.Children[2], MouseButton.Right, modifiers: KeyModifiers.Control);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Shift_Arrow_Key_Selects_Range()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
                Width = 100,
                Height = 100,
                SelectedIndex = 0,
            };

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(1).IsFocused);

            RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz", }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(2).IsFocused);

            RaiseKeyEvent(target, Key.Up, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(1).IsFocused);
        }

        [Fact]
        public void Shift_Down_Key_Selecting_Selects_Range_End_From_Focus()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
                Width = 100,
                Height = 100,
                SelectedIndex = 0,
            };

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ContainerFromIndex(1)!.Focus();
            RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(2).IsFocused);
        }

        [Fact]
        public void Shift_Down_Key_Selecting_Selects_Range_End_From_Focus_Moved_With_Ctrl_Key()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
                Width = 100,
                Height = 100,
                SelectedIndex = 0,
            };

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(1).IsFocused);

            RaiseKeyEvent(target, Key.Down, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(2).IsFocused);

            RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2, 3 }, SelectedContainers(target));
            Assert.True(target.ContainerFromIndex(3).IsFocused);
        }

        [Fact]
        public void SelectAll_Works_From_No_Selection_When_SelectedItem_Is_Bound_TwoWay()
        {
            // Issue #13676
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
                Width = 100,
                Height = 100,
            };

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.Bind(ListBox.SelectedItemProperty, new Binding("Tag") 
            { 
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            });

            target.SelectAll();

            Assert.Equal(new[] { 0, 1, 2, 3 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
        }

        [Fact]
        public void SelectAll_Works_From_No_Selection_When_SelectedIndex_Is_Bound_TwoWay()
        {
            // Issue #13676
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
                Width = 100,
                Height = 100,
            };

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.Bind(ListBox.SelectedIndexProperty, new Binding("Tag")
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            });

            target.SelectAll();

            Assert.Equal(new[] { 0, 1, 2, 3 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
        }

        private Control CreateListBoxTemplate(TemplatedControl parent, INameScope scope)
        {
            return new ScrollViewer
            {
                Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                }.RegisterInNameScope(scope)
            };
        }

        private Control CreateScrollViewerTemplate(TemplatedControl parent, INameScope scope)
        {
            return new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] =
                    ((Control)parent).GetObservable(ContentControl.ContentProperty).ToBinding(),
            }.RegisterInNameScope(scope);
        }

        private static void RaiseKeyEvent(Control target, Key key, KeyModifiers inputModifiers = 0)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static IEnumerable<int> SelectedContainers(SelectingItemsControl target)
        {
            return target.Presenter.Panel.Children
                .Select(x => x.Classes.Contains(":selected") ? target.IndexFromContainer(x) : -1)
                .Where(x => x != -1);
        }

    }
}
