using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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

                // 5. The previously selected ListBoxItem will appear in the ListBox
                var realizedItems = target.GetRealizedContainers()
                    .Cast<ListBoxItem>()
                    .Select(x => x.Content?.ToString())
                    .ToList();

                Assert.All(realizedItems, item => Assert.DoesNotContain(item, letters));
                Assert.Equal("0", realizedItems[0]);
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


        private Control CreateListBoxTemplate(TemplatedControl parent, INameScope scope)
        {
            return new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] = ((ListBox)parent).GetObservable(ItemsControl.ItemsPanelProperty).ToBinding(),
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
}
