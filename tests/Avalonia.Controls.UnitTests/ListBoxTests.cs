using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxTests : ScopedTestBase
    {
        private MouseTestHelper _mouse = new MouseTestHelper();

        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Item_Content()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            Prepare(target);

            var container = (ListBoxItem)target.Presenter.Panel.Children[0];
            Assert.IsType<Canvas>(container.Presenter.Child);
        }

        [Fact]
        public void ListBox_Should_Find_ItemsPresenter_In_ScrollViewer()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
            };

            Prepare(target);

            Assert.IsType<ItemsPresenter>(target.Presenter);
        }

        [Fact]
        public void ListBox_Should_Find_Scrollviewer_In_Template()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
            };

            ScrollViewer viewer = null;

            target.TemplateApplied += (sender, e) =>
            {
                viewer = target.Scroll as ScrollViewer;
            };

            Prepare(target);

            Assert.NotNull(viewer);
        }

        [Fact]
        public void ListBoxItem_Containers_Should_Be_Generated()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new[] { "Foo", "Bar", "Baz " };
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                };

                Prepare(target);

                var text = target.Presenter.Panel.Children
                    .OfType<ListBoxItem>()
                    .Select(x => x.Presenter.Child)
                    .OfType<TextBlock>()
                    .Select(x => x.Text)
                    .ToList();

                Assert.Equal(items, text);
            }
        }

        [Fact]
        public void Container_Should_Have_Theme_Set_To_ItemContainerTheme()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new[] { "Foo", "Bar", "Baz " };
                var theme = new ControlTheme(typeof(ListBoxItem));
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemContainerTheme = theme,
                };

                Prepare(target);

                var container = (ListBoxItem)target.Presenter.Panel.Children[0];

                Assert.Same(container.Theme, theme);
            }
        }

        [Fact]
        public void Inline_Item_Should_Have_Theme_Set_To_ItemContainerTheme()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var theme = new ControlTheme(typeof(ListBoxItem));
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    Items = { new ListBoxItem() },
                    ItemContainerTheme = theme,
                };

                Prepare(target);

                var container = (ListBoxItem)target.Presenter.Panel.Children[0];

                Assert.Same(container.Theme, theme);
            }
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set_For_DataTemplate_Generated_Items()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = new[] { "Foo", "Bar", "Baz " },
                };

                Prepare(target);

                Assert.Equal(3, target.GetLogicalChildren().Count());

                foreach (var child in target.GetLogicalChildren())
                {
                    Assert.IsType<ListBoxItem>(child);
                }
            }
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new object[]
                {
                    "Foo",
                    new Item("Bar"),
                    new TextBlock { Text = "Baz" },
                    new ListBoxItem { Content = "Qux" },
                };

                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    DataContext = "Base",
                    ItemTemplate = new FuncDataTemplate<Item>((x, _) => new Button { Content = x }),
                    ItemsSource = items,
                };

                Prepare(target);

                var dataContexts = target.Presenter.Panel.Children
                    .Cast<Control>()
                    .Select(x => x.DataContext)
                    .ToList();

                Assert.Equal(
                    new object[] { items[0], items[1], "Base", "Base" },
                    dataContexts);
            }
        }

        [Fact]
        public void Selection_Should_Be_Cleared_On_Recycled_Items()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                    SelectedIndex = 0,
                };

                Prepare(target);

                // Make sure we're virtualized and first item is selected.
                Assert.Equal(10, target.Presenter.Panel.Children.Count);
                Assert.True(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);

                // The selected item must not be the anchor, otherwise it won't get recycled.
                target.Selection.AnchorIndex = -1;

                // Scroll down a page.
                target.Scroll.Offset = new Vector(0, 10);
                Layout(target);

                // Make sure recycled item isn't now selected.
                Assert.False(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);
            }
        }

        [Fact]
        public void ScrollViewer_Should_Have_Correct_Extent_And_Viewport()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectedIndex = 0,
                };

                Prepare(target);

                Assert.Equal(new Size(100, 200), target.Scroll.Extent);
                Assert.Equal(new Size(100, 100), target.Scroll.Viewport);
            }
        }

        [Fact]
        public void Containers_Correct_After_Clear_Add_Remove()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                // Issue #1936
                var items = new AvaloniaList<string>(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                    SelectedIndex = 0,
                };

                Prepare(target);

                items.Clear();
                items.AddRange(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
                Layout(target);

                items.Remove("Item 2");
                Layout(target);

                var actual = target.GetRealizedContainers().Cast<ListBoxItem>().Select(x => (string)x.Content).ToList();
                Assert.Equal(items.OrderBy(x => x), actual.OrderBy(x => x));
            }
        }

        [Fact]
        public void Toggle_Selection_Should_Update_Containers()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    SelectionMode = SelectionMode.Toggle,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
                };

                Prepare(target);

                var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

                var item = lbItems[0];

                Assert.Equal(false, item.IsSelected);

                RaisePressedEvent(item, MouseButton.Left);

                Assert.Equal(true, item.IsSelected);

                RaisePressedEvent(item, MouseButton.Left);

                Assert.Equal(false, item.IsSelected);
            }
        }

        [Fact]
        public void Can_Decrease_Number_Of_Materialized_Items_By_Removing_From_Source_Collection()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = new AvaloniaList<string>(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
                };

                Prepare(target);
                target.Scroll.Offset = new Vector(0, 1);

                items.RemoveRange(0, 11);
            }
        }

        private void RaisePressedEvent(ListBoxItem item, MouseButton mouseButton)
        {
            _mouse.Click(item, item, mouseButton);
        }

        [Fact]
        public void LayoutManager_Should_Measure_Arrange_All()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = new AvaloniaList<string>(Enumerable.Range(1, 7).Select(v => v.ToString()));

                var wnd = new Window() { SizeToContent = SizeToContent.WidthAndHeight };

                wnd.IsVisible = true;

                var target = new ListBox();

                wnd.Content = target;

                var lm = wnd.LayoutManager;

                target.Height = 110;
                target.Width = 50;
                target.DataContext = items;

                target.ItemTemplate = new FuncDataTemplate<object>((c, _) =>
                {
                    var tb = new TextBlock() { Height = 10, Width = 30 };
                    tb.Bind(TextBlock.TextProperty, new Data.Binding());
                    return tb;
                }, true);

                lm.ExecuteInitialLayoutPass();

                target.ItemsSource = items;

                lm.ExecuteLayoutPass();

                items.Insert(3, "3+");
                lm.ExecuteLayoutPass();

                items.Insert(4, "4+");
                lm.ExecuteLayoutPass();

                //RESET
                items.Clear();
                foreach (var i in Enumerable.Range(1, 7))
                {
                    items.Add(i.ToString());
                }

                //working bit better with this line no outof memory or remaining to arrange/measure ???
                //lm.ExecuteLayoutPass();

                items.Insert(2, "2+");

                lm.ExecuteLayoutPass();
                //after few more layout cycles layoutmanager shouldn't hold any more visual for measure/arrange
                lm.ExecuteLayoutPass();
                lm.ExecuteLayoutPass();

                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var toMeasure = lm.GetType().GetField("_toMeasure", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.Layoutable>;
                var toArrange = lm.GetType().GetField("_toArrange", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.Layoutable>;

                Assert.Equal(0, toMeasure.Count());
                Assert.Equal(0, toArrange.Count());
            }
        }

        [Fact]
        public void ListBox_Should_Be_Valid_After_Remove_Of_Item_In_NonVisibleArea()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = new AvaloniaList<string>(Enumerable.Range(1, 30).Select(v => v.ToString()));

                var wnd = new Window() { Width = 100, Height = 100, IsVisible = true };

                var target = new ListBox()
                {
                    AutoScrollToSelectedItem = true,
                    Height = 100,
                    Width = 50,
                    ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border() { Height = 10 }),
                    ItemsSource = items,
                };
                wnd.Content = target;

                var lm = wnd.LayoutManager;

                lm.ExecuteInitialLayoutPass();

                //select last / scroll to last item
                target.SelectedItem = items.Last();

                lm.ExecuteLayoutPass();

                //remove the first item (in non realized area of the listbox)
                items.Remove("1");
                lm.ExecuteLayoutPass();

                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal("30", target.ContainerFromIndex(items.Count - 1).DataContext);
                Assert.Equal("29", target.ContainerFromIndex(items.Count - 2).DataContext);
                Assert.Equal("28", target.ContainerFromIndex(items.Count - 3).DataContext);
                Assert.Equal("27", target.ContainerFromIndex(items.Count - 4).DataContext);
                Assert.Equal("26", target.ContainerFromIndex(items.Count - 5).DataContext);
            }
        }

        [Fact]
        public void Clicking_Item_Should_Raise_BringIntoView_For_Correct_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                // Issue #3934
                var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                    SelectionMode = SelectionMode.AlwaysSelected,
                };

                Prepare(target);

                Threading.Dispatcher.UIThread.RunJobs();

                // First an item that is not index 0 must be selected.
                _mouse.Click(target.Presenter.Panel.Children[1]);

                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(1, target.Selection.AnchorIndex);

                // We're going to be clicking on item 9.
                var item = (ListBoxItem)target.Presenter.Panel.Children[9];
                var raised = 0;

                // Make sure a RequestBringIntoView event is raised for item 9. It won't be handled
                // by the ScrollContentPresenter as the item is already visible, so we don't need
                // handledEventsToo: true. Issue #3934 failed here because item 0 was being scrolled
                // into view due to SelectionMode.AlwaysSelected.
                target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) =>
                {

                    Assert.Same(item, e.TargetObject);
                    ++raised;
                });

                // Click item 9.
                _mouse.Click(item);

                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Adding_And_Selecting_Item_With_AutoScrollToSelectedItem_Should_NotHide_FirstItem()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = new AvaloniaList<string>();

                var wnd = new Window() { Width = 100, Height = 100, IsVisible = true };

                var target = new ListBox()
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    AutoScrollToSelectedItem = true,
                    Width = 50,
                    ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border() { Height = 10 }),
                    ItemsSource = items,
                };
                wnd.Content = target;

                var lm = wnd.LayoutManager;

                lm.ExecuteInitialLayoutPass();

                var panel = target.Presenter.Panel;

                items.Add("Item 1");
                target.Selection.Select(0);
                lm.ExecuteLayoutPass();

                Assert.Equal(1, panel.Children.Count);

                items.Add("Item 2");
                target.Selection.Select(1);
                lm.ExecuteLayoutPass();

                Assert.Equal(2, panel.Children.Count);

                //make sure we have enough space to show all items
                Assert.True(panel.Bounds.Height >= panel.Children.Sum(c => c.Bounds.Height));

                //make sure we show items and they completelly visible, not only partially
                Assert.True(panel.Children[0].Bounds.Top >= 0 && panel.Children[0].Bounds.Bottom <= panel.Bounds.Height, "first item is not completelly visible!");
                Assert.True(panel.Children[1].Bounds.Top >= 0 && panel.Children[1].Bounds.Bottom <= panel.Bounds.Height, "second item is not completelly visible!");
            }
        }

        [Fact]
        public void Initial_Binding_Of_SelectedItems_Should_Not_Cause_Write_To_SelectedItems()
        {
            var target = new ListBox
            {
                [!ListBox.ItemsSourceProperty] = new Binding("Items"),
                [!ListBox.SelectedItemsProperty] = new Binding("SelectedItems"),
            };

            var viewModel = new
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectedItems = new ObservableCollection<string> { "Bar" },
            };

            var raised = 0;

            viewModel.SelectedItems.CollectionChanged += (s, e) => ++raised;

            target.DataContext = viewModel;

            Assert.Equal(0, raised);
            Assert.Equal(new[] { "Bar" }, viewModel.SelectedItems);
            Assert.Equal(new[] { "Bar" }, target.SelectedItems);
            Assert.Equal(new[] { "Bar" }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Content_Can_Be_Bound_In_ItemContainerTheme()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new[] { new ItemViewModel("Foo"), new ItemViewModel("Bar") };
                var theme = new ControlTheme(typeof(ListBoxItem))
                {
                    Setters =
                    {
                        new Setter(ListBoxItem.ContentProperty, new Binding("Caption")),
                    }
                };

                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemContainerTheme = theme,
                };

                Prepare(target);

                var containers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();
                Assert.Equal(2, containers.Count);
                Assert.Equal("Foo", containers[0].Content);
                Assert.Equal("Bar", containers[1].Content);
            }
        }

        private static FuncControlTemplate ListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>((parent, scope) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding(),
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope));
        }

        private static FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
                }.RegisterInNameScope(scope));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                        new ScrollBar
                        {
                            Name = "verticalScrollBar",
                            Orientation = Orientation.Vertical,
                        }
                    }
                });
        }

        private static void Prepare(ListBox target)
        {
            target.Width = target.Height = 100;

            var root = new TestRoot(target)
            {
                Resources =
                {
                    { 
                        typeof(ListBoxItem),
                        new ControlTheme(typeof(ListBoxItem))
                        {
                            Setters = { new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()) }
                        }
                    }
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static void Layout(Control c)
        {
            ((ILayoutRoot)c.GetVisualRoot()).LayoutManager.ExecuteLayoutPass();
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        [Fact]
        public void SelectedItem_Validation()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
                SelectionMode = SelectionMode.AlwaysSelected,
            };

            Prepare(target);

            var exception = new System.InvalidCastException("failed validation");
            var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
            target.Bind(ComboBox.SelectedItemProperty, textObservable);

            Assert.True(DataValidationErrors.GetHasErrors(target));
            Assert.True(DataValidationErrors.GetErrors(target).SequenceEqual(new[] { exception }));
        }

        [Fact]
        public void Handles_Resetting_Items()
        {
            var items = new ResettingCollection(100);
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas { Height = 10 }),
            };

            Prepare(target);

            var realized = target.GetRealizedContainers()
                .Cast<ListBoxItem>()
                .Select(x => (string)x.DataContext)
                .ToList();

            Assert.Equal(Enumerable.Range(0, 10).Select(x => $"Item{x}"), realized);

            items.Reverse();
            Layout(target);

            realized = target.GetRealizedContainers()
                .Cast<ListBoxItem>()
                .Select(x => (string)x.DataContext)
                .ToList();

            Assert.Equal(Enumerable.Range(0, 10).Select(x => $"Item{99 - x}"), realized);
        }

        [Fact]
        public void Handles_Resetting_Items_With_Existing_Selection_And_AutoScrollToSelectedItem()
        {
            var items = new ResettingCollection(100);
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas { Height = 10 }),
                AutoScrollToSelectedItem = true,
                SelectedIndex = 1,
            };

            Prepare(target);

            var realized = target.GetRealizedContainers()
                .Cast<ListBoxItem>()
                .Select(x => (string)x.DataContext)
                .ToList();

            Assert.Equal(Enumerable.Range(0, 10).Select(x => $"Item{x}"), realized);

            items.Reverse();
            Layout(target);

            Threading.Dispatcher.UIThread.RunJobs();

            realized = target.GetRealizedContainers()
                .Cast<ListBoxItem>()
                .Select(x => (string)x.DataContext)
                .ToList();

            // "Item1" should remain selected, and now be at the bottom of the viewport.
            Assert.Equal(Enumerable.Range(0, 10).Select(x => $"Item{10 - x}"), realized);
        }

        [Fact]
        public void Arrow_Keys_Should_Move_Selection_Vertical()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            RaiseKeyEvent(target, Key.Down);
            Assert.Equal(1, target.SelectedIndex);

            RaiseKeyEvent(target, Key.Down);
            Assert.Equal(2, target.SelectedIndex);

            RaiseKeyEvent(target, Key.Up);
            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Arrow_Keys_Should_Move_Selection_Horizontal()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel 
                {
                    Orientation = Orientation.Horizontal 
                }),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            RaiseKeyEvent(target, Key.Right);
            Assert.Equal(1, target.SelectedIndex);

            RaiseKeyEvent(target, Key.Right);
            Assert.Equal(2, target.SelectedIndex);

            RaiseKeyEvent(target, Key.Left);
            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Arrow_Keys_Should_Focus_Selection()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            RaiseKeyEvent(target, Key.Down);
            Assert.True(target.ContainerFromIndex(1).IsFocused);

            RaiseKeyEvent(target, Key.Down);
            Assert.True(target.ContainerFromIndex(2).IsFocused);

            RaiseKeyEvent(target, Key.Up);
            Assert.True(target.ContainerFromIndex(1).IsFocused);
        }

        [Fact]
        public void Down_Key_Selecting_From_No_Selection_And_No_Focus_Selects_From_Start()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
                Width = 100,
                Height = 100,
            };

            Prepare(target);

            RaiseKeyEvent(target, Key.Down);

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Down_Key_Selecting_From_No_Selection_Selects_From_Focus()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
                Width = 100,
                Height = 100,
            };

            Prepare(target);

            target.ContainerFromIndex(1)!.Focus();
            RaiseKeyEvent(target, Key.Down);

            Assert.Equal(2, target.SelectedIndex);
        }

        [Fact]
        public void Ctrl_Down_Key_Moves_Focus_But_Not_Selection()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
                Width = 100,
                Height = 100,
                SelectedIndex = 0,
            };

            Prepare(target);

            target.ContainerFromIndex(0)!.Focus();
            RaiseKeyEvent(target, Key.Down, KeyModifiers.Control);

            Assert.Equal(0, target.SelectedIndex);
            Assert.True(target.ContainerFromIndex(1).IsFocused);
        }

        [Fact]
        public void Down_Key_Brings_Unrealized_Selection_Into_View()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var items = Enumerable.Range(0, 100).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                Width = 100,
                Height = 100,
                SelectedIndex = 0,
            };

            Prepare(target);

            target.ContainerFromIndex(0)!.Focus();
            target.Scroll.Offset = new Vector(0, 100);
            Layout(target);

            var panel = (VirtualizingStackPanel)target.ItemsPanelRoot;
            Assert.Equal(10, panel.FirstRealizedIndex);

            RaiseKeyEvent(target, Key.Down);

            Assert.Equal(1, target.SelectedIndex);
            Assert.True(target.ContainerFromIndex(1).IsFocused);
            Assert.Equal(new Vector(0, 10), target.Scroll.Offset);
        }

        [Fact]
        public void WrapSelection_Should_Wrap()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                    WrapSelection = true
                };

                Prepare(target);

                var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

                var first = lbItems.First();
                var beforeLast = lbItems[^2];
                var last = lbItems.Last();

                first.Focus();

                RaisePressedEvent(first, MouseButton.Left);
                Assert.Equal(true, first.IsSelected);

                RaiseKeyEvent(target, Key.Up);
                Assert.Equal(true, last.IsSelected);

                RaiseKeyEvent(target, Key.Up);
                Assert.Equal(true, beforeLast.IsSelected);

                RaiseKeyEvent(target, Key.Down);
                Assert.Equal(true, last.IsSelected);

                RaiseKeyEvent(target, Key.Down);
                Assert.Equal(true, first.IsSelected);

                target.WrapSelection = false;
                RaiseKeyEvent(target, Key.Up);

                Assert.Equal(true, first.IsSelected);
            }
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Each_Item_Container_On_Layout()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = { "Foo", "Bar", "Baz" },
            };

            var result = new List<Control>();
            var index = 0;

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(index++, e.Index);
                result.Add(e.Container);
            };

            Prepare(target);

            Assert.Equal(3, result.Count);
            Assert.Equal(target.GetRealizedContainers(), result);
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Each_ItemsSource_Container_On_Layout()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = new[] { "Foo", "Bar", "Baz" },
            };

            var result = new List<Control>();
            var index = 0;

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(index++, e.Index);
                result.Add(e.Container);
            };

            Prepare(target);

            Assert.Equal(3, result.Count);
            Assert.Equal(target.GetRealizedContainers(), result);
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Added_Item()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var data = new AvaloniaList<string> { "Foo", "Bar", "Baz" };
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = data,
            };

            Prepare(target);

            var result = new List<Control>();

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(3, e.Index);
                result.Add(e.Container);
            };

            data.Add("Qux");
            Layout(target);

            Assert.Equal(1, result.Count);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_When_Item_Added()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var data = new AvaloniaList<string> { "Foo", "Bar", "Baz" };
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = data,
            };

            Prepare(target);

            var result = new List<Control>();
            var index = 1;

            target.ContainerIndexChanged += (s, e) =>
            {
                Assert.Equal(index++, e.OldIndex);
                Assert.Equal(index, e.NewIndex);
                result.Add(e.Container);
            };

            data.Insert(1, "Qux");
            Layout(target);

            Assert.Equal(2, result.Count);
            Assert.Equal(target.GetRealizedContainers().Skip(2), result);
        }

        [Fact]
        public void ContainerClearing_Is_Raised_When_Item_Removed()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var data = new AvaloniaList<string> { "Foo", "Bar", "Baz" };
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = data,
            };

            Prepare(target);

            var expected = target.ContainerFromIndex(1);
            var raised = 0;

            target.ContainerClearing += (s, e) =>
            {
                Assert.Same(expected, e.Container);
                ++raised;
            };

            data.RemoveAt(1);
            Layout(target);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Tab_Navigation_Should_Move_To_First_Item_When_No_Anchor_Element_Selected()
        {
            var services = TestServices.StyledWindow.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice());
            using var app = UnitTestApplication.Start(services);

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = { "Foo", "Bar", "Baz" },
            };

            var button = new Button 
            { 
                Content = "Button",
                [DockPanel.DockProperty] = Dock.Top,
            };

            var root = new TestRoot
            {
                Child = new DockPanel
                {
                    Children =
                    {
                        button,
                        target,
                    }
                }
            };

            var navigation = new KeyboardNavigationHandler();
            navigation.SetOwner(root);

            root.LayoutManager.ExecuteInitialLayoutPass();

            button.Focus();
            RaiseKeyEvent(button, Key.Tab);

            var item = target.ContainerFromIndex(0);
            Assert.Same(item, root.FocusManager.GetFocusedElement());
        }

        [Fact]
        public void Tab_Navigation_Should_Move_To_Anchor_Element()
        {
            var services = TestServices.StyledWindow.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice());
            using var app = UnitTestApplication.Start(services);

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = { "Foo", "Bar", "Baz" },
            };

            var button = new Button
            {
                Content = "Button",
                [DockPanel.DockProperty] = Dock.Top,
            };

            var root = new TestRoot
            {
                Width = 1000,
                Height = 1000,
                Child = new DockPanel
                {
                    Children =
                    {
                        button,
                        target,
                    }
                }
            };

            var navigation = new KeyboardNavigationHandler();
            navigation.SetOwner(root);

            root.LayoutManager.ExecuteInitialLayoutPass();

            button.Focus();
            target.Selection.AnchorIndex = 1;
            RaiseKeyEvent(button, Key.Tab);

            var item = target.ContainerFromIndex(1);
            Assert.Same(item, root.FocusManager.GetFocusedElement());

            RaiseKeyEvent(item, Key.Tab);

            Assert.Same(button, root.FocusManager.GetFocusedElement());

            target.Selection.AnchorIndex = 2;
            RaiseKeyEvent(button, Key.Tab);

            item = target.ContainerFromIndex(2);
            Assert.Same(item, root.FocusManager.GetFocusedElement());
        }

        [Fact]
        public void Reads_Only_Realized_Items_From_ItemsSource()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var data = new DataVirtualizingList();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                ItemsSource = data,
            };

            Prepare(target);

            var panel = Assert.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
            Assert.Equal(0, panel.FirstRealizedIndex);
            Assert.Equal(9, panel.LastRealizedIndex);

            Assert.Equal(
                Enumerable.Range(0, 10).Select(x => $"Item{x}"),
                data.GetRealizedItems());
        }

        [Fact]
        public void Should_Not_Handle_Space_When_TextBox_Inside_ListBoxItem()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new TextBox
                {
                    Focusable = true
                };
                var listbox = new ListBox()
                {
                    Template = ListBoxTemplate(),
                    Items =
                    {
                        new ListBoxItem()
                        {
                            Template = ListBoxItemTemplate(),
                            Content = target,
                        }
                    }
                };

                var nKeyDown = 0;

                var root = new TestRoot()
                {
                    Width = 1000,
                    Height = 1000,
                    Child = listbox,
                };

                root.KeyDown += (s, e) => nKeyDown++;


                listbox.ApplyTemplate();
                root.LayoutManager.ExecuteInitialLayoutPass();

                target.Focus();

                RaiseKeyEvent(target, Key.Space, KeyModifiers.None);

                Assert.Equal(1, nKeyDown);
            }
        }

        [Fact]
        public void ListBoxItem_Should_Not_Block_Tapped_Events()
        {

            // #13474
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {

                Pointer _pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
                ulong nextStamp = 1;

                var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    ItemsSource = items,
                    SelectionMode = SelectionMode.Toggle,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
                };

                Prepare(target);

                var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

                var item = lbItems[0];

                int tappedCount = 0;
                target.Tapped += (s, e) =>
                {
                    tappedCount++;
                };

                _mouse.Click(item);
                Assert.Equal(1, tappedCount);

                // Raise PointerPressed and PointerReleased events with the Left Button pressed.  TouchTestHelper 
                // assumes no button pressed, which prevents it from generating Tapped events, or I would use that.

                item.RaiseEvent(new PointerPressedEventArgs(item, _pointer, (Visual)item, default, nextStamp++,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));


                item.RaiseEvent(new PointerReleasedEventArgs(item, _pointer, (Visual)item, default, nextStamp++,
                    PointerPointProperties.None, KeyModifiers.None, MouseButton.Left));

                Assert.Equal(2, tappedCount);
            }
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

        private record ItemViewModel(string Caption);

        private class ResettingCollection : List<string>, INotifyCollectionChanged
        {
            public ResettingCollection(int itemCount)
            {
                AddRange(Enumerable.Range(0, itemCount).Select(x => $"Item{x}"));
            }

            public new void Reverse()
            {
                base.Reverse();
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;
        }

        private class DataVirtualizingList : IList
        {
            private readonly List<string> _inner = new(Enumerable.Repeat<string>(null, 100));

            public object this[int index] 
            { 
                get => _inner[index] = $"Item{index}"; 
                set => throw new NotSupportedException();
            }

            public IEnumerable<string> GetRealizedItems() => _inner.Where(x => x is not null);
            public bool IsFixedSize => true;
            public bool IsReadOnly => true;
            public int Count => _inner.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public int Add(object value) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(object value) => throw new NotImplementedException();
            public void CopyTo(Array array, int index) => throw new NotImplementedException();
            public IEnumerator GetEnumerator() => _inner.GetEnumerator();
            public int IndexOf(object value) => throw new NotImplementedException();
            public void Insert(int index, object value) => throw new NotSupportedException();
            public void Remove(object value) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
        }
    }
}
