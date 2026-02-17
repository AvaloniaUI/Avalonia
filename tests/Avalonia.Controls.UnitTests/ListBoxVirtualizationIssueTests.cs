using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
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
                target.Scroll!.Offset = new Avalonia.Vector(0, 300); // Scroll down 6 items
                target.UpdateLayout();

                // Verify that the first item is no longer realized
                var realizedContainers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();
                // Assert.DoesNotContain(realizedContainers, x => x.Content as string == "A");

                // 3. Click the Button to change ItemsSource
                target.ItemsSource = numbers;
                target.UpdateLayout();

                // 4. Scroll to the top
                target.Scroll.Offset = new Avalonia.Vector(0, 0);
                target.UpdateLayout();

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
                    Template = new FuncControlTemplate(CreateListBoxTemplate),
                    ItemsSource = letters,
                    ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 50 }),
                    Height = 100, // Show 2 items (100 / 50 = 2)
                    ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { CacheLength = 0 }),
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
