using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_AutoSelect : ScopedTestBase
    {
        [Fact]
        public void First_Item_Should_Be_Selected()
        {
            var target = new TestSelector
            {
                ItemsSource = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void First_Item_Should_Be_Selected_When_Added()
        {
            var items = new AvaloniaList<string>();
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }


        [Fact]
        public void First_Item_Should_Be_Selected_When_Reset()
        {
            var items = new ResetOnAdd();
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Item_Should_Be_Selected_When_Selection_Removed()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar", "baz", "qux" });

            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;
            items.RemoveAt(2);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_No_Items_Left()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });

            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;
            items.RemoveAt(1);
            items.RemoveAt(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Removing_Selected_First_Item_Should_Select_Next_Item()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();
            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("bar", target.SelectedItem);
        }

        [Fact]
        public void First_Visible_Item_Should_Be_Selected_When_First_Container_Is_Hidden()
        {
            // Uses own-container items (Controls) so that IsVisible can be set before preparation.
            var target = new TestSelector
            {
                ItemsSource = new object[]
                {
                    new ListBoxItem { Content = "hidden", IsVisible = false },
                    new ListBoxItem { Content = "visible" },
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void First_Enabled_Item_Should_Be_Selected_When_First_Container_Is_Disabled()
        {
            var target = new TestSelector
            {
                ItemsSource = new object[]
                {
                    new ListBoxItem { Content = "disabled", IsEnabled = false },
                    new ListBoxItem { Content = "enabled" },
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void First_Visible_Item_Should_Be_Selected_When_Container_Becomes_Hidden_During_Preparation()
        {
            // Regression test for https://github.com/AvaloniaUI/Avalonia/pull/20798
            // Simulates a MVVM scenario where container visibility is set by a binding applied
            // during PrepareContainerForItemOverride (e.g. an ItemContainerTheme). Verifies that
            // selection lands on the first truly visible container rather than an unrealized item.
            var target = new TestSelectorHidingFirstContainers(hiddenCount: 2)
            {
                ItemsSource = new[] { "item-0", "item-1", "item-2" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            Assert.Equal(2, target.SelectedIndex);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_All_Containers_Are_Hidden_During_Preparation()
        {
            // When all containers are made invisible during preparation (MVVM binding scenario),
            // SelectedIndex must be -1 rather than the last container's index. Previously the
            // selection would cascade to the last unrealized item and land on an invisible one.
            var target = new TestSelectorHidingFirstContainers(hiddenCount: 3)
            {
                ItemsSource = new[] { "item-0", "item-1", "item-2" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter!.ApplyTemplate();

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void AutoScrollToSelectedItem_Should_Work_When_Becoming_Visible()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList();

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
                    Height = 100,
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                    AutoScrollToSelectedItem = true,
                    IsVisible = false
                };

                target.Width = target.Height = 100;
                var root = new TestRoot(target);
                root.LayoutManager.ExecuteInitialLayoutPass();

                // Select item 50
                target.SelectedIndex = 50;

                // Make visible
                target.IsVisible = true;
                target.UpdateLayout();
            
                // Wait for dispatcher
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                target.UpdateLayout();

                var scrollViewer = (ScrollViewer)target.VisualChildren[0];
                var offset = scrollViewer.Offset.Y;

                // Item 50 is at 50 * 50 = 2500. 
                // ListBox height is 100, so it should be visible if offset is between 2400 and 2500.
                Assert.InRange(offset, 2400, 2500);
            }
        }
        
        [Fact]
        public void AutoScrollToSelectedItem_Should_Work_When_Ancestor_Becomes_Visible()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList();

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
                    Height = 100,
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                    AutoScrollToSelectedItem = true,
                };

                target.Width = target.Height = 100;

                var host = new StackPanel
                {
                    IsVisible = false,
                    Children =
                    {
                        target,
                    },
                };

                var root = new TestRoot(host);
                root.LayoutManager.ExecuteInitialLayoutPass();

                target.SelectedIndex = 50;
                Assert.False(target.IsEffectivelyVisible);

                host.IsVisible = true;
                root.LayoutManager.ExecuteLayoutPass();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                root.LayoutManager.ExecuteLayoutPass();

                var scrollViewer = (ScrollViewer)target.VisualChildren[0];
                var offset = scrollViewer.Offset.Y;
                Assert.InRange(offset, 2400, 2500);
            }
        }
        
        private static FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestSelector : SelectingItemsControl
        {
            static TestSelector()
            {
                SelectionModeProperty.OverrideDefaultValue<TestSelector>(SelectionMode.AlwaysSelected);
            }
        }

        private class ResetOnAdd : List<string>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public new void Add(string item)
            {
                base.Add(item);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// A selector with AlwaysSelected that hides the first N containers during preparation.
        /// This simulates a MVVM scenario where an ItemContainerTheme binding sets
        /// IsVisible=false on some containers.
        /// </summary>
        private class TestSelectorHidingFirstContainers : SelectingItemsControl
        {
            private readonly int _hiddenCount;

            public TestSelectorHidingFirstContainers(int hiddenCount)
            {
                _hiddenCount = hiddenCount;
            }

            static TestSelectorHidingFirstContainers()
            {
                SelectionModeProperty.OverrideDefaultValue<TestSelectorHidingFirstContainers>(SelectionMode.AlwaysSelected);
            }

            protected internal override void PrepareContainerForItemOverride(Control container, object? item, int index)
            {
                base.PrepareContainerForItemOverride(container, item, index);
                if (index < _hiddenCount)
                    container.IsVisible = false;
            }
        }
        
        private Control CreateListBoxTemplate(TemplatedControl parent, INameScope scope)
        {
            return new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] =
                        ((ListBox)parent).GetObservable(ItemsControl.ItemsPanelProperty).ToBinding(),
                }.RegisterInNameScope(scope)
            }.RegisterInNameScope(scope);
        }
        private Control CreateScrollViewerTemplate(TemplatedControl parent, INameScope scope)
        {
            return new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] =
                    parent.GetObservable(ContentControl.ContentProperty).ToBinding(),
            }.RegisterInNameScope(scope);
        }

        
    }
}
