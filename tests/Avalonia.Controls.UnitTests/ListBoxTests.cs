using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxTests
    {
        private MouseTestHelper _mouse = new MouseTestHelper();
        
        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Item_Content()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = new[] { "Foo" },
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
                    Items = items,
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
        public void LogicalChildren_Should_Be_Set_For_DataTemplate_Generated_Items()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    Items = new[] { "Foo", "Bar", "Baz " },
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
                    DataTemplates =
                    {
                        new FuncDataTemplate<Item>((x, _) => new Button { Content = x })
                    },
                    Items = items,
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
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            // Make sure we're virtualized and first item is selected.
            Assert.Equal(10, target.Presenter.Panel.Children.Count);
            Assert.True(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);

            // Scroll down a page.
            target.Scroll.Offset = new Vector(0, 10);

            // Make sure recycled item isn't now selected.
            Assert.False(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);
        }

        [Fact]
        public void ScrollViewer_Should_Have_Correct_Extent_And_Viewport()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            Assert.Equal(new Size(20, 20), target.Scroll.Extent);
            Assert.Equal(new Size(100, 10), target.Scroll.Viewport);
        }

        [Fact]
        public void Containers_Correct_After_Clear_Add_Remove()
        {
            // Issue #1936
            var items = new AvaloniaList<string>(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            items.Clear();
            items.AddRange(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            items.Remove("Item 2");

            Assert.Equal(
                items,
                target.Presenter.Panel.Children.Cast<ListBoxItem>().Select(x => (string)x.Content));
        }

        [Fact]
        public void Toggle_Selection_Should_Update_Containers()
        {
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                SelectionMode = SelectionMode.Toggle,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
            };

            Prepare(target);

            var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

            var item = lbItems[0];

            Assert.Equal(false, item.IsSelected);

            RaisePressedEvent(target, item, MouseButton.Left);

            Assert.Equal(true, item.IsSelected);

            RaisePressedEvent(target, item, MouseButton.Left);

            Assert.Equal(false, item.IsSelected);
        }

        [Fact]
        public void Can_Decrease_Number_Of_Materialized_Items_By_Removing_From_Source_Collection()
        {
            var items = new AvaloniaList<string>(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
            };

            Prepare(target);
            target.Scroll.Offset = new Vector(0, 1);

            items.RemoveRange(0, 11);
        }

        private void RaisePressedEvent(ListBox listBox, ListBoxItem item, MouseButton mouseButton)
        {
            _mouse.Click(listBox, item, mouseButton);
        }

        [Fact]
        public void ListBox_After_Scroll_IndexOutOfRangeException_Shouldnt_Be_Thrown()
        {
            var items = Enumerable.Range(0, 11).Select(x => $"{x}").ToArray();

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 11 })
            };

            Prepare(target);

            var panel = target.Presenter.Panel as IVirtualizingPanel;

            var listBoxItems = panel.Children.OfType<ListBoxItem>();

            //virtualization should have created exactly 10 items
            Assert.Equal(10, listBoxItems.Count());
            Assert.Equal("0", listBoxItems.First().DataContext);
            Assert.Equal("9", listBoxItems.Last().DataContext);

            //instead pixeloffset > 0 there could be pretty complex sequence for repro
            //it involves add/remove/scroll to end multiple actions
            //which i can't find so far :(, but this is the simplest way to add it to unit test
            panel.PixelOffset = 1;

            //here scroll to end -> IndexOutOfRangeException is thrown
            target.Scroll.Offset = new Vector(0, 2);

            Assert.True(true);
        }

        [Fact]
        public void LayoutManager_Should_Measure_Arrange_All()
        {
            var virtualizationMode = ItemVirtualizationMode.Simple;
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
                target.VirtualizationMode = virtualizationMode;

                target.ItemTemplate = new FuncDataTemplate<object>((c, _) =>
                {
                    var tb = new TextBlock() { Height = 10, Width = 30 };
                    tb.Bind(TextBlock.TextProperty, new Data.Binding());
                    return tb;
                }, true);

                lm.ExecuteInitialLayoutPass();

                target.Items = items;

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
                var toMeasure = lm.GetType().GetField("_toMeasure", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.ILayoutable>;
                var toArrange = lm.GetType().GetField("_toArrange", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.ILayoutable>;

                Assert.Equal(0, toMeasure.Count());
                Assert.Equal(0, toArrange.Count());
            }
        }

        [Fact]
        public void Clicking_Item_Should_Raise_BringIntoView_For_Correct_Control()
        {
            // Issue #3934
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectionMode = SelectionMode.AlwaysSelected,
                VirtualizationMode = ItemVirtualizationMode.None,
            };

            Prepare(target);

            // First an item that is not index 0 must be selected.
            _mouse.Click(target.Presenter.Panel.Children[1]);
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

            Assert.Equal(1, raised);
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
                    VerticalAlignment = Layout.VerticalAlignment.Top,
                    AutoScrollToSelectedItem = true,
                    Width = 50,
                    VirtualizationMode = ItemVirtualizationMode.Simple,
                    ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border() { Height = 10 }),
                    Items = items,
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
                [!ListBox.ItemsProperty] = new Binding("Items"),
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

        private FuncControlTemplate ListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>((parent, scope) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty).ToBinding(),
                        [~ItemsPresenter.ItemsPanelProperty] = parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding(),
                        [~ItemsPresenter.VirtualizationModeProperty] = parent.GetObservable(ListBox.VirtualizationModeProperty).ToBinding(),
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope));
        }

        private FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
                }.RegisterInNameScope(scope));
        }

        private FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                            [~ScrollContentPresenter.ContentProperty] = parent.GetObservable(ScrollViewer.ContentProperty).ToBinding(),
                            [~~ScrollContentPresenter.ExtentProperty] = parent[~~ScrollViewer.ExtentProperty],
                            [~~ScrollContentPresenter.OffsetProperty] = parent[~~ScrollViewer.OffsetProperty],
                            [~~ScrollContentPresenter.ViewportProperty] = parent[~~ScrollViewer.ViewportProperty],
                        }.RegisterInNameScope(scope),
                        new ScrollBar
                        {
                            Name = "verticalScrollBar",
                            [~ScrollBar.MaximumProperty] = parent[~ScrollViewer.VerticalScrollBarMaximumProperty],
                            [~~ScrollBar.ValueProperty] = parent[~~ScrollViewer.VerticalScrollBarValueProperty],
                        }
                    }
                });
        }

        private void Prepare(ListBox target)
        {
            // The ListBox needs to be part of a rooted visual tree.
            var root = new TestRoot();
            root.Child = target;

            // Apply the template to the ListBox itself.
            target.ApplyTemplate();

            // Then to its inner ScrollViewer.
            var scrollViewer = (ScrollViewer)target.GetVisualChildren().Single();
            scrollViewer.ApplyTemplate();

            // Then make the ScrollViewer create its child.
            ((ContentPresenter)scrollViewer.Presenter).UpdateChild();

            // Now the ItemsPresenter should be reigstered, so apply its template.
            target.Presenter.ApplyTemplate();

            // Because ListBox items are virtualized we need to do a layout to make them appear.
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            // Now set and apply the item templates.
            foreach (ListBoxItem item in target.Presenter.Panel.Children)
            {
                item.Template = ListBoxItemTemplate();
                item.ApplyTemplate();
                item.Presenter.ApplyTemplate();
                ((ContentPresenter)item.Presenter).UpdateChild();
            }

            // The items were created before the template was applied, so now we need to go back
            // and re-arrange everything.
            foreach (IControl i in target.GetSelfAndVisualDescendants())
            {
                i.InvalidateMeasure();
            }

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));
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
                Items = new[] { "Foo" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
                SelectionMode = SelectionMode.AlwaysSelected,
                VirtualizationMode = ItemVirtualizationMode.None
            };

            Prepare(target);
            
            var exception = new System.InvalidCastException("failed validation");
            var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
            target.Bind(ComboBox.SelectedItemProperty, textObservable);
                
            Assert.True(DataValidationErrors.GetHasErrors(target));
            Assert.True(DataValidationErrors.GetErrors(target).SequenceEqual(new[] { exception }));
        }
    }
}
