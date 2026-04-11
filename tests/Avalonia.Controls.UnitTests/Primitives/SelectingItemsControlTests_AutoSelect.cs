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
