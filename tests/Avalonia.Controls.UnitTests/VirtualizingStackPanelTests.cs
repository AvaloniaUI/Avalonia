using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class VirtualizingStackPanelTests : ScopedTestBase
    {
        [Fact]
        public void Creates_Initial_Items()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedItems(target, itemsControl, 0, 10);
        }

        [Fact]
        public void Initializes_Initial_Control_Items()
        {
            using var app = App();
            var items = Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10});
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null);

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedControlItems<Button>(target, itemsControl, 0, 10);
        }

        [Fact]
        public void Creates_Reassigned_Items()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(items: Array.Empty<object>());

            Assert.Empty(itemsControl.GetRealizedContainers());

            itemsControl.ItemsSource = new[] { "foo", "bar" };
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 2);
        }

        [Fact]
        public void Scrolls_Down_One_Item()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.Offset = new Vector(0, 10);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 1, 10);
        }

        [Fact]
        public void Scrolls_Down_More_Than_A_Page()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 20, 10);
        }

        [Fact]
        public void Scrolls_Down_To_Index()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, 11, 10);
        }

        [Fact]
        public void Scrolls_Up_To_Index()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.ScrollToEnd();
            Layout(target);

            Assert.Equal(90, target.FirstRealizedIndex);

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, 20, 10);
        }

        [Fact]
        public void Scrolling_Up_To_Index_Does_Not_Create_A_Page_Of_Unrealized_Elements()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.ScrollToEnd();
            Layout(target);
            target.ScrollIntoView(20);

            Assert.Equal(11, target.Children.Count);
        }

        [Fact]
        public void Creates_Elements_On_Item_Insert_1()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            items.Insert(0, "new");

            Assert.Equal(11, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(new[] { -1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[0] = target.GetRealizedElements().ElementAt(0);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Creates_Elements_On_Item_Insert_2()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            items.Insert(2, "new");

            Assert.Equal(11, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[2] = target.GetRealizedElements().ElementAt(2);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Updates_Elements_On_Item_Remove()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toRecycle = target.GetRealizedElements().ElementAt(2);
            items.RemoveAt(2);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.Equal(Enumerable.Range(0, 9), indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // And the removed row should now have been recycled as the last row.
            elements.Add(toRecycle);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Updates_Elements_On_Item_Replace()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toReplace = target.GetRealizedElements().ElementAt(2);
            items[2] = "new";

            // Container being replaced should have been recycled.
            Assert.DoesNotContain(toReplace, target.GetRealizedElements());
            Assert.False(toReplace!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9 }, indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);
        }

        [Fact]
        public void Updates_Elements_On_Item_Move()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toMove = target.GetRealizedElements().ElementAt(2);
            items.Move(2, 6);

            // Container being moved should have been recycled.
            Assert.DoesNotContain(toMove, target.GetRealizedElements());
            Assert.False(toMove!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, -1, 7, 8, 9 }, indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);
        }

        [Fact]
        public void Removes_Control_Items_From_Panel_On_Item_Remove()
        {
            using var app = App();
            var items = new ObservableCollection<Button>(Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 }));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null);

            Assert.Equal(1000, scroll.Extent.Height);

            var removed = items[1];
            items.RemoveAt(1);

            Assert.Null(removed.Parent);
            Assert.Null(removed.VisualParent);
        }

        [Fact]
        public void Does_Not_Recycle_Focused_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(target.GetRealizedElements().First()!.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
        }

        [Fact]
        public void Removing_Item_Of_Focused_Element_Clears_Focus()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(focused.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
            Assert.All(target.GetRealizedElements(), x => Assert.NotSame(focused, x));
        }

        [Fact]
        public void Scrolling_Back_To_Focused_Element_Uses_Correct_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(focused.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            Assert.Same(focused, target.GetRealizedElements().First());
        }

        [Fact]
        public void Focusing_Another_Element_Recycles_Original_Focus_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var originalFocused = target.GetRealizedElements().First()!;
            originalFocused.Focusable = true;
            originalFocused.Focus();

            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var newFocused = target.GetRealizedElements().First()!;
            newFocused.Focusable = true;
            newFocused.Focus();

            Assert.False(originalFocused.IsVisible);
        }

        [Fact]
        public void Removing_Range_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.RemoveRange(0, 80);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 10, 10);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Fact]
        public void Removing_Range_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.RemoveRange(0, 95);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Fact]
        public void Resetting_Collection_To_Have_Less_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.Reset(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, 10, 10);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Fact]
        public void Resetting_Collection_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.Reset(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Fact]
        public void NthChild_Selector_Works()
        {
            using var app = App();
            
            var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();
            
            Assert.Equal(10, realized.Count);
            
            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 4 || i == 9) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        [Fact]
        public void NthLastChild_Selector_Works()
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(10, realized.Count);

            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 0 || i == 5) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_When_Scrolling()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var raised = 0;

            itemsControl.ContainerPrepared += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(10, raised);
        }

        [Fact]
        public void ContainerClearing_Is_Raised_When_Scrolling()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var raised = 0;

            itemsControl.ContainerClearing += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(10, raised);
        }

        [Fact]
        public void Scrolling_Down_With_Larger_Element_Does_Not_Cause_Jump_And_Arrives_At_End()
        {
            using var app = App();

            var items = Enumerable.Range(0, 1000).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
                new Canvas
                {
                    Width = 100,
                    [!Canvas.HeightProperty] = new Binding("Height"),
                });

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: itemTemplate);

            var index = target.FirstRealizedIndex;

            // Scroll down to the larger element.
            while (target.LastRealizedIndex < items.Count - 1)
            {
                scroll.LineDown();
                Layout(target);

                Assert.True(
                    target.FirstRealizedIndex >= index, 
                    $"{target.FirstRealizedIndex} is not greater or equal to {index}");

                if (scroll.Offset.Y + scroll.Viewport.Height == scroll.Extent.Height)
                    Assert.Equal(items.Count - 1, target.LastRealizedIndex);

                index = target.FirstRealizedIndex;
            }
        }

        [Fact]
        public void Scrolling_Up_To_Larger_Element_Does_Not_Cause_Jump()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
                new Canvas
                {
                    Width = 100,
                    [!Canvas.HeightProperty] = new Binding("Height"),
                });

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: itemTemplate);

            // Scroll past the larger element.
            scroll.Offset = new Vector(0, 600);
            Layout(target);

            // Precondition checks
            Assert.True(target.FirstRealizedIndex > 20);

            var index = target.FirstRealizedIndex;

            // Scroll up to the top.
            while (scroll.Offset.Y > 0)
            {
                scroll.LineUp();
                Layout(target);

                Assert.True(target.FirstRealizedIndex <= index, $"{target.FirstRealizedIndex} is not less than {index}");
                index = target.FirstRealizedIndex;
            }
        }
        
        [Fact]
        public void Scrolling_Up_To_Smaller_Element_Does_Not_Cause_Jump()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 30)).ToList();
            items[20].Height = 25;

            var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
                new Canvas
                {
                    Width = 100,
                    [!Canvas.HeightProperty] = new Binding("Height"),
                });

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: itemTemplate);

            // Scroll past the larger element.
            scroll.Offset = new Vector(0, 25 * items[0].Height);
            Layout(target);

            // Precondition checks
            Assert.True(target.FirstRealizedIndex > 20);

            var index = target.FirstRealizedIndex;

            // Scroll up to the top.
            while (scroll.Offset.Y > 0)
            {
                scroll.Offset = scroll.Offset - new Vector(0, 5);
                Layout(target);

                Assert.True(
                    target.FirstRealizedIndex <= index, 
                    $"{target.FirstRealizedIndex} is not less than {index}");
                Assert.True(
                    index - target.FirstRealizedIndex <= 1,
                    $"FirstIndex changed from {index} to {target.FirstRealizedIndex}");
                
                index = target.FirstRealizedIndex;
            }
        }

        [Fact]
        public void Does_Not_Throw_When_Estimating_Viewport_With_Ancestor_Margin()
        {
            // Issue #11272
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<ItemsControl>();
            var container = new Decorator { Margin = new Thickness(100) };
            var root = new TestRoot(true, container);
            
            root.LayoutManager.ExecuteInitialLayoutPass();

            container.Child = itemsControl;

            root.LayoutManager.ExecuteLayoutPass();
        }

        [Fact]
        public void Supports_Null_Recycle_Key_When_Scrolling()
        {
            using var app = App();
            var (_, scroll, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>();
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            scroll.Offset = new(0, 20);

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.DoesNotContain(firstItem, itemsControl.ItemsPanelRoot!.Children);
        }

        [Fact]
        public void Supports_Null_Recycle_Key_When_Clearing_Items()
        {
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>();
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            itemsControl.ItemsSource = null;

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.Empty(itemsControl.ItemsPanelRoot!.Children);            
        }

        [Fact]
        public void ScrollIntoView_On_Effectively_Invisible_Panel_Does_Not_Create_Ghost_Elements()
        {
            var items = new[] { "foo", "bar", "baz" };
            var (target, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(items: items);
            var container = new Decorator { Margin = new Thickness(100), Child = itemsControl };
            var root = new TestRoot(true, container);

            root.LayoutManager.ExecuteInitialLayoutPass();

            // Clear the items and do a layout to recycle all elements.
            itemsControl.ItemsSource = null;
            root.LayoutManager.ExecuteLayoutPass();

            // Should have no realized elements and 3 unrealized elements.
            Assert.Equal(0, target.GetRealizedElements().Count);
            Assert.Equal(3, target.Children.Count);

            // Make the panel effectively invisible and set items.
            container.IsVisible = false;
            itemsControl.ItemsSource = items;

            // Try to scroll into view while effectively invisible.
            target.ScrollIntoView(0);

            // Make the panel visible and layout.
            container.IsVisible = true;
            root.LayoutManager.ExecuteLayoutPass();

            // Should have 3 realized elements and no unrealized elements.
            Assert.Equal(3, target.GetRealizedElements().Count);
            Assert.Equal(3, target.Children.Count);
        }

        private static IReadOnlyList<int> GetRealizedIndexes(VirtualizingStackPanel target, ItemsControl itemsControl)
        {
            return target.GetRealizedElements()
                .Select(x => x is null ? -1 : itemsControl.IndexFromContainer((Control)x))
                .ToList();
        }

        private static void AssertRealizedItems(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            int firstIndex,
            int count)
        {
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(target, x.VisualParent));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(itemsControl, x.Parent));

            var childIndexes = target.GetRealizedContainers()!
                .Select(x => itemsControl.IndexFromContainer(x))
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(Enumerable.Range(firstIndex, count), childIndexes);
        }

        private static void AssertRealizedControlItems<TContainer>(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            int firstIndex,
            int count)
        {
            Assert.All(target.GetRealizedContainers()!, x => Assert.IsType<TContainer>(x));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(target, x.VisualParent));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(itemsControl, x.Parent));

            var childIndexes = target.GetRealizedContainers()!
                .Select(x => itemsControl.IndexFromContainer(x))
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(Enumerable.Range(firstIndex, count), childIndexes);
        }

        private static (VirtualizingStackPanel, ScrollViewer, ItemsControl) CreateTarget(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            IEnumerable<Style>? styles = null)
        {
            var (target, scroll, itemsControl) = CreateUnrootedTarget<ItemsControl>(items, itemTemplate);
            var root = CreateRoot(itemsControl, styles);

            root.LayoutManager.ExecuteInitialLayoutPass();

            return (target, scroll, itemsControl);
        }

        private static (VirtualizingStackPanel, ScrollViewer, T) CreateUnrootedTarget<T>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default)
                where T : ItemsControl, new()
        {
            var target = new VirtualizingStackPanel();

            items ??= new ObservableCollection<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));

            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            var scroll = new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Content = presenter,
                Template = ScrollViewerTemplate(),
            };

            var itemsControl = new T
            {
                ItemsSource = items,
                Template = new FuncControlTemplate<ItemsControl>((_, ns) => scroll.RegisterInNameScope(ns)),
                ItemsPanel = new FuncTemplate<Panel?>(() => target),
                ItemTemplate = itemTemplate.GetValueOrDefault(DefaultItemTemplate()),
            };

            return (target, scroll, itemsControl);
        }

        private static TestRoot CreateRoot(Control? child, IEnumerable<Style>? styles = null)
        {
            var root = new TestRoot(true, child);
            root.ClientSize = new(100, 100);

            if (styles is not null)
                root.Styles.AddRange(styles);

            return root;
        }

        private static IDataTemplate DefaultItemTemplate()
        {
            return new FuncDataTemplate<object>((x, _) => new Canvas { Width = 100, Height = 10 });
        }

        private static void Layout(Control target)
        {
            var root = (ILayoutRoot?)target.GetVisualRoot();
            root?.LayoutManager.ExecuteLayoutPass();
        }

        private static IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((x, ns) =>
                new ScrollContentPresenter
                {
                    Name = "PART_ContentPresenter",
                }.RegisterInNameScope(ns));
        }

        private static IDisposable App() => UnitTestApplication.Start(TestServices.RealFocus);

        private class ItemWithHeight
        {
            public ItemWithHeight(int index, double height = 10)
            {
                Caption = $"Item {index}";
                Height = height;
            }
            
            public string Caption { get; set; }
            public double Height { get; set; }
        }

        private class ResettingCollection : List<string>, INotifyCollectionChanged
        {
            public ResettingCollection(IEnumerable<string> items)
            {
                AddRange(items);
            }

            public void Reset(IEnumerable<string> items)
            {
                Clear();
                AddRange(items);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public event NotifyCollectionChangedEventHandler? CollectionChanged;
        }

        private class NonRecyclingItemsControl : ItemsControl
        {
            protected override Type StyleKeyOverride => typeof(ItemsControl);

            protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
            {
                recycleKey = null;
                return true;
            }
        }
    }
}
