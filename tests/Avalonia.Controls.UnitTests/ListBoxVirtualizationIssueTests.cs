using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxVirtualizationIssueTests : ScopedTestBase
    {
        [Fact]
        public void Replaced_ItemsSource_Should_Not_Show_Old_Selected_Item_When_Scrolled_Back()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var letters = "ABCDEFGHIJ".Select(c => c.ToString()).ToList();
                var numbers = "0123456789".Select(c => c.ToString()).ToList();

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = letters,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 50 }),
                    Height = 100, // Show 2 items
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                };

                Prepare(target);

                // 1. Select a ListBoxItem
                target.SelectedIndex = 0;
                Assert.True(((ListBoxItem)target.Presenter!.Panel!.Children[0]).IsSelected);

                // 2. Scroll until the selected ListBoxItem is no longer visible
                target.ScrollIntoView(letters.Count - 1); // Scroll down to the last item

                // Verify that the first item is no longer realized
                var realizedContainers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();
                Assert.DoesNotContain(realizedContainers, x => x.Content as string == "A");

                // 3. Change the ItemsSource
                target.ItemsSource = numbers;

                // 4. Scroll to the top
                target.ScrollIntoView(0);

                // 5. The previously selected ListBoxItem should NOT appear in the ListBox
                var realizedItems = target.GetRealizedContainers()
                    .Cast<ListBoxItem>()
                    .Select(x => x.Content?.ToString())
                    .ToList();

                Assert.All(realizedItems, item => Assert.DoesNotContain(item, letters));
                Assert.Equal("0", realizedItems[0]);
            }
        }

        [Fact]
        public void AddingItemsAtTopShouldNotCreateGhostItems()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                ObservableCollection<Item> items = new();
                for (int i = 0; i < 100; i++)
                {
                    items.Add(new Item(i));
                }

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = items,
                    Height = 100, // Show 2 items
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
                };

                Prepare(target);

                // Scroll to some position
                var scrollViewer = (ScrollViewer)((Visual)target).VisualChildren[0];
                scrollViewer.Offset = new Vector(0, 500); // Scrolled down
                target.UpdateLayout();

                // Add items at the top multiple times
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        items.Insert(0, new Item(1000 + (i * 100 + j)));
                    }

                    target.UpdateLayout();

                    // Randomly select something
                    target.SelectedIndex = items.Count - 1;
                    target.UpdateLayout();

                    // Scroll a bit
                    scrollViewer.ScrollToEnd();
                    scrollViewer.ScrollToEnd();
                    target.UpdateLayout();

                    // Check for ghost items during the process
                    var p = target.Presenter!.Panel!;
                    var vChildren = p.Children.Where(c => c.IsVisible).ToList();
                    var rContainers = target.GetRealizedContainers().ToList();

                    // Only visible children should be considered. Invisible children may be recycled items kept for reuse.
                    Assert.Equal(rContainers.Count, vChildren.Count);
                    foreach (var child in vChildren)
                    {
                        Assert.Contains(child, rContainers);
                    }

                    scrollViewer.ScrollToHome();
                    
                    var realizedContainers = target.GetRealizedContainers()
                        .Cast<ListBoxItem>()
                        .ToList();

                    var realizedItems = realizedContainers
                        .Select(x => x.Content)
                        .Cast<Item>()
                        .ToList();

                    // Check for duplicates in realized items
                    var duplicateIds = realizedItems.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => g.Key)
                        .ToList();
                    Assert.Empty(duplicateIds);

                    // Check if all realized items are actually in the items source
                    foreach (var item in realizedItems)
                    {
                        Assert.Contains(item, items);
                    }

                    // Check if realized items are in the correct order
                    int lastIndex = -1;
                    foreach (var item in realizedItems)
                    {
                        int currentIndex = items.IndexOf(item);
                        Assert.True(currentIndex > lastIndex,
                            $"Item {item.Id} is at index {currentIndex}, but previous item was at index {lastIndex}");
                        lastIndex = currentIndex;
                    }

                    // New check: verify that all visual children of the panel are accounted for in realizedContainers
                    var panel = target.Presenter!.Panel!;
                    var visualChildren = panel.Children.ToList();

                    // Realized containers should match exactly the visual children of the panel
                    // (VirtualizingStackPanel manages its children such that they should be the realized containers)
                    // We also check if all children are visible, if not they might be "ghosts"
                    foreach (var child in visualChildren)
                    {
                        Assert.True(child.IsVisible, $"Child {((ListBoxItem)child).Content} should be visible");
                    }

                    Assert.Equal(realizedContainers.Count, visualChildren.Count);
                    foreach (var child in visualChildren)
                    {
                        Assert.Contains(child, realizedContainers);
                    }
                }
            }
        }

        [Fact]
        public void RealizedContainers_Should_Only_Include_Visible_Items_With_CacheLength_Zero()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var letters = "ABCDEFGHIJ".Select(c => c.ToString()).ToList();

                var target = new ListBox
                {
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = letters,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 50 }),
                    Height = 100, // Show 2 items (100 / 50 = 2)
                };

                Prepare(target);

                // At the top, only 2 items should be visible (items at index 0 and 1)
                var realizedContainers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();

                // With CacheLength = 0, we should only have the visible items realized
                Assert.Equal(2, realizedContainers.Count);
                Assert.Equal("A", realizedContainers[0].Content?.ToString());
                Assert.Equal("B", realizedContainers[1].Content?.ToString());
            }
        }


        [Fact]
        public void GhostItemTest_FocusManagement()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(i => $"Item {i}"));

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 50 }),
                    Height = 100, // Show 2 items
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                };

                Prepare(target);

                // 1. Get the first container and focus it
                var container = (ListBoxItem)target.Presenter!.Panel!.Children[0];
                KeyboardNavigation.SetTabOnceActiveElement(target, container);

                // 2. Scroll down so the first item is recycled
                target.ScrollIntoView(10);
                target.UpdateLayout();

                // 3. Verify it is now _focusedElement in the panel
                var panel = (VirtualizingStackPanel)target.Presenter!.Panel!;
                
                var vChildren = panel.Children.ToList();
                var realizedContainers = target.GetRealizedContainers().ToList();
                
                // The focused container should still be in Children, but NOT in realizedContainers
                Assert.Contains(container, panel.Children);
                Assert.DoesNotContain(container, realizedContainers);

                // Is it visible? If it's the ghost, it might be visible.
                // In RecycleElement, if it becomes _focusedElement, IsVisible is NOT set to false.
                Assert.True(container.IsVisible, "Focused element should remain visible in the current implementation (potential ghost)");

                // Now scroll back to top.
                target.ScrollIntoView(0);
                target.UpdateLayout();

                // Check if we have two containers for the same item or other weirdness
                var visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();
                // If it was a ghost, it might still be there or we might have two items for the same thing
                Assert.Equal(target.GetRealizedContainers().Count(), visibleChildren.Count);

                // 4. Test: Re-insert at top might cause issues if _focusedElement is not updated correctly
                items.Insert(0, "New Item");
                target.UpdateLayout();

                visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();
                Assert.Equal(target.GetRealizedContainers().Count(), visibleChildren.Count);

                // 5. Remove the focused item while it's recycled
                target.ScrollIntoView(10);
                target.UpdateLayout();
                Assert.Contains(container, panel.Children);
                
                items.RemoveAt(1); // Item 0 was at index 1 because of Insert(0, "New Item")
                target.UpdateLayout();
                
                // container should be removed from children because RecycleElementOnItemRemoved is called
                Assert.DoesNotContain(container, panel.Children);
                Assert.False(container.IsVisible);
            }
        }

        [Fact]
        public void GhostItemTest_ScrollToManagement()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(i => $"Item {i}"));

                var target = new ListBox
                {
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = items,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 50 }),
                    Height = 100, // Show 2 items
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
                };

                Prepare(target);

                // 1. ScrollIntoView to trigger _scrollToElement
                // We use a high index and don't call UpdateLayout immediately if we want to catch it in between?
                // Actually ScrollIntoView calls layout internally.
                target.ScrollIntoView(50);
                
                var panel = (VirtualizingStackPanel)target.Presenter!.Panel!;
                var realizedContainers = target.GetRealizedContainers().ToList();
                
                // 2. Remove the item we just scrolled to
                items.RemoveAt(50);
                target.UpdateLayout();
                
                // If it was kept in _scrollToElement and not recycled, it might be a ghost.
                var visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();
                Assert.Equal(target.GetRealizedContainers().Count(), visibleChildren.Count);
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

        private static void Prepare(ListBox target)
        {
            target.Width = target.Height = 100;
            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();
        }
    }

    public class EvenItem : Item
    {
        public EvenItem(int id) : base(id) { }
    }

    public class UnEvenItem : Item
    {
        public UnEvenItem(int id) : base(id) { }
    }

    public class Item
    {
        public Item(int id)
        {
            _id = id;
            Message = GenerateMessage();
        }

        private int _id;
        public int Id => _id;

        public string Name { get; set; }

        public string Message { get; set; }

        public DateTime TimeStamp { get; set; }

        private string GenerateMessage(
            int minWords = 3, int maxWords = 20,
            int minSentences = 1, int maxSentences = 10)
        {
            var words = new[]
            {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer", "adipiscing", "elit", "sed", "diam",
                "nonummy", "nibh", "euismod", "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
            };

            var rand = new Random();
            int numSentences = rand.Next(maxSentences - minSentences) + minSentences + 1;
            int numWords = rand.Next(maxWords - minWords) + minWords + 1;

            var result = new StringBuilder();

            for (int s = 0; s < numSentences; s++)
            {
                for (int w = 0; w < numWords; w++)
                {
                    if (w > 0)
                    {
                        result.Append(" ");
                    }

                    result.Append(words[rand.Next(words.Length)]);
                }

                result.Append(". ");
            }

            return result.ToString();
        }

        public override string ToString() => Message;
    }
}
