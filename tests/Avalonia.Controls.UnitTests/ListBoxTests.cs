// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
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
            using (Application())
            {
                var target = new ListBox
                {
                    Items = new[] { "Foo" },
                    ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
                };

                Prepare(target);

                var container = (ListBoxItem)target.Presenter.Panel.Children[0];
                Assert.IsType<Canvas>(container.Presenter.Child);
            }
        }

        [Fact]
        public void ListBox_Should_Find_ItemsPresenter_In_ScrollViewer()
        {
            using (Application())
            {
                var target = new ListBox();

                Prepare(target);

                Assert.IsType<ItemsRepeaterPresenter>(target.Presenter);
            }
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
            using (Application())
            {
                var items = new[] { "Foo", "Bar", "Baz " };
                var target = new ListBox
                {
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
            using (Application())
            {
                var target = new ListBox
                {
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
            using (Application())
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
        public void Should_Not_Attempt_To_Recycle_Inline_ListBoxItems()
        {
            using (Application())
            {
                // Our items here are ListBoxItems which should be included in the list
                // without creating another container around them.
                var items = Enumerable.Range(0, 20)
                    .Select(x => new ListBoxItem { Content = $"Item {x}", Height = 10 })
                    .ToList();

                var target = new ListBox
                {
                    DataContext = "Base",
                    DataTemplates =
                    {
                        new FuncDataTemplate<Item>((x, _) => new Button { Content = x })
                    },
                    Items = items,
                };

                Prepare(target);

                Assert.Equal(items.Take(11), target.Presenter.Panel.Children);

                ScrollToVerticalOffset(target, 100);

                // All items should now be realized even though we have virtualization enabled
                // because it's not possible to recycle inline items.
                Assert.Equal(items, target.Presenter.Panel.Children);
            }
        }

        [Fact]
        public void Selection_Should_Be_Cleared_On_Recycled_Items()
        {
            using (Application())
            {
                var target = new ListBox
                {
                    Items = Enumerable.Range(0, 30).Select(x => $"Item {x}").ToList(),
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                    SelectedIndex = 0,
                };

                Prepare(target);

                // Make sure we're virtualized and first item is selected.
                Assert.Equal(11, target.Presenter.Panel.Children.Count);
                Assert.True(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);

                // Scroll down a more than a page.
                ScrollToVerticalOffset(target, 150);

                // Make sure recycled item isn't now selected.
                Assert.False(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);
            }
        }

        [Fact]
        public void ScrollViewer_Should_Have_Correct_Extent_And_Viewport()
        {
            using (Application())
            {
                var target = new ListBox
                {
                    Template = ListBoxTemplate(),
                    Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
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
            using (Application())
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
                items.AddRange(Enumerable.Range(0, 12).Select(x => $"Item {x}"));
                items.Remove("Item 2");
                Layout(target);

                var result = target.Presenter.Panel.Children
                    .Cast<ListBoxItem>()
                    .Select(x => (string)x.Content)
                    .ToList();
                Assert.Equal(items, result);
            }
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

        [Fact]
        public void Toggle_Selection_Should_Update_Containers()
        {
            using (Application())
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
        }

        [Fact]
        public void LayoutManager_Should_Measure_Arrange_All()
        {
            var virtualizationMode = ItemVirtualizationMode.Simple;
            using (Application())
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

                lm.ExecuteInitialLayoutPass(wnd);

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

        private FuncControlTemplate ListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>((parent, scope) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Content = new ItemsRepeaterPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        VerticalCacheLength = 0,
                        [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty).ToBinding(),
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

        private IDisposable Application()
        {
            var services = new TestServices(
                renderInterface: new MockPlatformRenderInterface(),
                styler: new Styler(),
                windowingPlatform: new MockWindowingPlatform());
            return UnitTestApplication.Start(services);
        }

        private void Prepare(ListBox target)
        {
            // Create the root with the required styles.
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<ListBox>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ListBoxTemplate()),
                        },
                    },
                    new Style(x => x.OfType<ScrollViewer>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ScrollViewerTemplate()),
                        },
                    },
                    new Style(x => x.OfType<ListBoxItem>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()),
                        },
                    },
                },
                Child = target,
            };

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
            var rect = new Rect(0, 0, 100, 100);
            target.Presenter.TransformedBounds = new TransformedBounds(rect, rect, Matrix.Identity);
            root.Measure(rect.Size);
            root.Arrange(rect);
        }

        private void Layout(ListBox target)
        {
            ((ILayoutRoot)target.GetVisualRoot()).LayoutManager.ExecuteLayoutPass();
        }

        private void ScrollToVerticalOffset(ListBox target, double offset)
        {
            var rect = target.Presenter.TransformedBounds.Value.Bounds.WithY(offset);

            target.Scroll.Offset = target.Scroll.Offset.WithY(offset);
            target.Presenter.TransformedBounds = new TransformedBounds(rect, rect, Matrix.Identity);
            Layout(target);
        }

        private void RaisePressedEvent(ListBox listBox, ListBoxItem item, MouseButton mouseButton)
        {
            _mouse.Click(listBox, item, mouseButton);
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
