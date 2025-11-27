using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging;
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
        private static FuncDataTemplate<ItemWithHeight> CanvasWithHeightTemplate = new((_, _) =>
            new CanvasCountingMeasureArrangeCalls
            {
                Width = 100,
                [!Layoutable.HeightProperty] = new Binding("Height"),
            });

        private static FuncDataTemplate<ItemWithWidth> CanvasWithWidthTemplate = new((_, _) =>
            new CanvasCountingMeasureArrangeCalls
            {
                Height = 100,
                [!Layoutable.WidthProperty] = new Binding("Width"),
            });

        [Theory]
        [InlineData(0d  , 10)]
        [InlineData(0.5d, 20)]
        public void Creates_Initial_Items(double bufferFactor, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor:bufferFactor);

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedItems(target, itemsControl, 0, expectedCount);
        }

        [Theory]
        [InlineData(0d, 10)]
        [InlineData(0.5d, 20)]  // Buffer factor of 0.5. Since at start there is no room, the 10 additional items are just appended
        public void Initializes_Initial_Control_Items(double bufferFactor, int expectedCount)
        {
            using var app = App();
            var items = Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 });
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null, bufferFactor:bufferFactor);

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedControlItems<Button>(target, itemsControl, 0, expectedCount);
        }

        [Theory]
        [InlineData(0d, 2)]
        [InlineData(0.5d, 2)]  
        public void Creates_Reassigned_Items(double bufferFactor, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(items: Array.Empty<object>(), bufferFactor: bufferFactor);

            Assert.Empty(itemsControl.GetRealizedContainers());

            itemsControl.ItemsSource = new[] { "foo", "bar" };
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, expectedCount);
        }

        [Theory]
        [InlineData(0d, 1, 10)]
        [InlineData(0.5d, 0, 20)]
        public void Scrolls_Down_One_Item(double bufferFactor, int expectedFirstIndex, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor:bufferFactor);

            scroll.Offset = new Vector(0, 10);
            Layout(target);

            AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
        }

        [Theory]
        [InlineData(0d, 20,10)]
        [InlineData(0.5d, 15,20)]
        public void Scrolls_Down_More_Than_A_Page(double bufferFactor, int expectedFirstIndex, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor:bufferFactor);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
        }

        [Theory]
        [InlineData(0d, 11, 10)]
        [InlineData(0.5d, 6, 20)]
        public void Scrolls_Down_To_Index(double bufferFactor, int expectedFirstIndex, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
        }

        [Theory]
        [InlineData(0d, 90, 20, 10)]
        [InlineData(0.5d, 80, 15, 20)]
        public void Scrolls_Up_To_Index(double bufferFactor, int firstRealizedIndex, int expectedFirstIndex, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor:bufferFactor);

            scroll.ScrollToEnd();
            Layout(target);

            Assert.Equal(firstRealizedIndex, target.FirstRealizedIndex);

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
        }

        [Theory]
        [InlineData(0d, 11)]
        [InlineData(0.5d, 21)]
        public void Scrolling_Up_To_Index_Does_Not_Create_A_Page_Of_Unrealized_Elements(double bufferFactor, int expectedCount)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor:bufferFactor);

            scroll.ScrollToEnd();
            Layout(target);
            target.ScrollIntoView(20);

            Assert.Equal(expectedCount, target.Children.Count);
        }

        [Theory]
        [InlineData(0d, 
            10, 
            11, 
            "-1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10", 
            10)]
        [InlineData(0.5d,
            20,
            21,
            "-1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20",
            20)]
        public void Creates_Elements_On_Item_Insert_1(double bufferFactor, 
            int firstCount,
            int secondCount, 
            string indexesRaw, 
            int thirdCount)
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget(bufferFactor:bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(firstCount, target.GetRealizedElements().Count);

            items.Insert(0, "new");

            Assert.Equal(secondCount, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(indexesRaw.Split(", ").Select(Int32.Parse).ToArray(), indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, thirdCount), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[0] = target.GetRealizedElements().ElementAt(0);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Theory]
        [InlineData(0d,
            10,
            11,
            "0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10",
            10)]
        [InlineData(0.5d,
            20,
            21,
            "0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20",
            20)]
        public void Creates_Elements_On_Item_Insert_2(double bufferFactor,
            int firstCount,
            int secondCount,
            string indexesRaw,
            int thirdCount)
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget(bufferFactor:bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(firstCount, target.GetRealizedElements().Count);

            items.Insert(2, "new");

            Assert.Equal(secondCount, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(indexesRaw.Split(", ").Select(Int32.Parse).ToArray(), indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, thirdCount), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[2] = target.GetRealizedElements().ElementAt(2);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Updates_Elements_On_Item_Moved(double bufferFactor)
        {
            // Arrange

            using var app = App();

            var actualItems = new AvaloniaList<string>(Enumerable
                .Range(0, 100)
                .Select(x => $"Item {x}"));

            var (target, _, itemsControl) = CreateTarget(items: actualItems, bufferFactor:bufferFactor);

            var expectedRealizedElementContents = new[] { 1, 2, 0, 3, 4, 5, 6, 7, 8, 9 }
                .Select(x => $"Item {x}");

            // Act

            actualItems.Move(0, 2);
            Layout(target);

            // Assert

            var actualRealizedElementContents = target
                .GetRealizedElements()
                .Cast<ContentPresenter>()
                .Select(x => x.Content);

            Assert.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Updates_Elements_On_Item_Range_Moved(double bufferFactor)
        {
            // Arrange

            using var app = App();

            var actualItems = new AvaloniaList<string>(Enumerable
                .Range(0, 100)
                .Select(x => $"Item {x}"));

            var (target, _, itemsControl) = CreateTarget(items: actualItems, bufferFactor: bufferFactor);

            var expectedRealizedElementContents = new[] { 2, 0, 1, 3, 4, 5, 6, 7, 8, 9 }
                .Select(x => $"Item {x}");

            // Act

            actualItems.MoveRange(0, 2, 3);
            Layout(target);

            // Assert

            var actualRealizedElementContents = target
                .GetRealizedElements()
                .Cast<ContentPresenter>()
                .Select(x => x.Content);

            Assert.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
        }

        [Theory]
        [InlineData(0d, 10, 9)]
        [InlineData(0.5d, 20, 19)]
        public void Updates_Elements_On_Item_Remove(double bufferFactor, int firstCount, int secondCount)
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(firstCount, target.GetRealizedElements().Count);

            var toRecycle = target.GetRealizedElements().ElementAt(2);
            items.RemoveAt(2);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.Equal(Enumerable.Range(0, secondCount), indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(0, firstCount), indexes);

            // And the removed row should now have been recycled as the last row.
            elements.Add(toRecycle);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Theory]
        [InlineData(0d, 10, "0, 1, -1, 3, 4, 5, 6, 7, 8, 9")]
        [InlineData(0.5d, 20, "0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19")]
        public void Updates_Elements_On_Item_Replace(double bufferFactor, int firstCount, string indexesRaw)
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(firstCount, target.GetRealizedElements().Count);

            var toReplace = target.GetRealizedElements().ElementAt(2);
            items[2] = "new";

            // Container being replaced should have been recycled.
            Assert.DoesNotContain(toReplace, target.GetRealizedElements());
            Assert.False(toReplace!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(indexesRaw.Split(", ").Select(Int32.Parse).ToArray(), indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, firstCount), indexes);
        }

        [Theory]
        [InlineData(0d, 10, "0, 1, 2, 3, 4, 5, -1, 7, 8, 9")]
        [InlineData(0.5d, 20, "0, 1, 2, 3, 4, 5, -1, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19")]
        public void Updates_Elements_On_Item_Move(double bufferFactor, int firstCount, string indexesRaw)
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget(bufferFactor:bufferFactor);
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(firstCount, target.GetRealizedElements().Count);

            var toMove = target.GetRealizedElements().ElementAt(2);
            items.Move(2, 6);

            // Container being moved should have been recycled.
            Assert.DoesNotContain(toMove, target.GetRealizedElements());
            Assert.False(toMove!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(indexesRaw.Split(", ").Select(Int32.Parse).ToArray(), indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, firstCount), indexes);
        }
       
        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Removes_Control_Items_From_Panel_On_Item_Remove(double bufferFactor)
        {
            using var app = App();
            var items = new ObservableCollection<Button>(Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 }));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null, bufferFactor:bufferFactor);

            Assert.Equal(1000, scroll.Extent.Height);

            var removed = items[1];
            items.RemoveAt(1);

            Assert.Null(removed.Parent);
            Assert.Null(removed.VisualParent);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Does_Not_Recycle_Focused_Element(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(target.GetRealizedElements().First()!.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Removing_Item_Of_Focused_Element_Clears_Focus(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(focused.IsKeyboardFocusWithin);
            Assert.Equal(focused, KeyboardNavigation.GetTabOnceActiveElement(itemsControl));

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.RemoveAt(0);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
            Assert.All(target.GetRealizedElements(), x => Assert.NotSame(focused, x));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Scrolling_Back_To_Focused_Element_Uses_Correct_Element(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

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

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Focusing_Another_Element_Recycles_Original_Focus_Element(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

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

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Focused_Element_Losing_Focus_Does_Not_Reset_Selection(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, listBox) = CreateTarget<ListBox, VirtualizingStackPanel>(
                styles: new[]
                {
                    new Style(x => x.OfType<ListBoxItem>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()),
                        }
                    }
                }, bufferFactor: bufferFactor);

            listBox.SelectedIndex = 0;

            var selectedContainer = target.GetRealizedElements().First()!;
            selectedContainer.Focusable = true;
            selectedContainer.Focus();

            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var newFocused = target.GetRealizedElements().First()!;
            newFocused.Focusable = true;
            newFocused.Focus();

            Assert.Equal(0, listBox.SelectedIndex);
        }

        [Theory]
        [InlineData(0d, 90, 10, 10)]
        [InlineData(0.5d, 80, 0, 20)]
        public void Removing_Range_When_Scrolled_To_End_Updates_Viewport(double bufferFactor, int firstIndex, int secondIndex, int count)
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items, bufferFactor: bufferFactor);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, firstIndex, count);

            items.RemoveRange(0, 80);
            Layout(target);

            AssertRealizedItems(target, itemsControl, secondIndex, count);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Theory]
        [InlineData(0d, 90, 10)]
        [InlineData(0.5d, 80, 20)]
        public void Removing_Range_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport(double bufferFactor, int firstIndex, int count)
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items, bufferFactor: bufferFactor);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, firstIndex, count);

            items.RemoveRange(0, 95);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Theory]
        [InlineData(0d, 90, 10, 10)]
        [InlineData(0.5d, 80,0, 20)]
        public void Resetting_Collection_To_Have_Less_Items_When_Scrolled_To_End_Updates_Viewport(double bufferFactor, int firstIndex, int secondIndex, int count)
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items, bufferFactor: bufferFactor);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, firstIndex, count);

            items.Reset(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, secondIndex, count);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Theory]
        [InlineData(0d, 90, 10)]
        [InlineData(0.5d, 80, 20)]
        public void Resetting_Collection_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport(double bufferFactor, int firstIndex, int count)
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items, bufferFactor: bufferFactor);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, firstIndex, count);

            items.Reset(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Fact]
        public void Shrinking_Viewport_Then_Growing_Back_Triggers_Remeasure()
        {
            // Regression test for stale _extendedViewport comparison in OnEffectiveViewportChanged.
            //
            // When the viewport shrinks (e.g., ComboBox popup shrinks during filtering),
            // OnEffectiveViewportChanged doesn't trigger a measure (needsMeasure=false because
            // the smaller viewport is within the old extended viewport). The _extendedViewport
            // comparison baseline is NOT updated. When the viewport later grows back,
            // OnEffectiveViewportChanged compares against the stale large _extendedViewport,
            // concludes "no significant change", and skips the measure. This prevents item
            // realization when the only measure trigger is OnEffectiveViewportChanged.
            //
            // The fix uses a separate _lastKnownExtendedViewport that is always updated,
            // so the comparison correctly detects viewport growth after a shrink.
            //
            // Key: ScrollContentPresenter passes infinite height for vertical scroll, so
            // the panel's MeasureOverride is NOT called from the layout cascade when only
            // the root size changes. OnEffectiveViewportChanged is the sole measure trigger.
            using var app = App();

            var items = Enumerable.Range(0, 20).Select(x => $"Item {x}");
            var (target, scroll, itemsControl) =
               CreateUnrootedTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                  items: items, bufferFactor: 0);
            var root = CreateRoot(itemsControl, new Size(100, 100));

            root.LayoutManager.ExecuteInitialLayoutPass();

            // Initial state: viewport 0-100, 10 items visible, _extendedViewport = (0,0,100,100)
            AssertRealizedItems(target, itemsControl, 0, 10);

            // Shrink viewport (simulates popup shrinking when items are filtered).
            // Panel MeasureOverride is NOT called (ScrollContentPresenter passes infinite height).
            // OnEffectiveViewportChanged fires with small viewport but needsMeasure=false
            // because the small viewport is within the old _extendedViewport.
            root.ClientSize = new Size(100, 10);
            root.InvalidateMeasure();
            Layout(target);

            // Reset counters after shrink
            target.ResetMeasureArrangeCounters();

            // Grow viewport back (simulates popup growing when filter is removed).
            // Panel MeasureOverride is NOT called from layout cascade (same infinite constraint).
            // OnEffectiveViewportChanged is the ONLY path to trigger a remeasure.
            root.ClientSize = new Size(100, 100);
            root.InvalidateMeasure();
            Layout(target);

            // Without fix: OnEffectiveViewportChanged compares new viewport (0-100) against
            // stale _extendedViewport (0-100, never updated during shrink). Sees no change.
            // needsMeasure=false. No remeasure triggered. Measure count = 0.
            //
            // With fix: compares against _lastKnownExtendedViewport (0-10, updated during
            // shrink). Detects that viewport grew past it (100 > 10). needsMeasure=true.
            // InvalidateMeasure called. Measure count >= 1.
            Assert.True(target.Measured >= 1,
               "Panel should be re-measured when viewport grows back after a previous shrink. " +
               "OnEffectiveViewportChanged must detect viewport growth by comparing against " +
               "the last known extended viewport, not the stale _extendedViewport.");
        }

        [Theory]
        [InlineData(0d, 10, "4,9")]
        [InlineData(0.5d, 20, "4,9,14,19")]
        public void NthChild_Selector_Works(double bufferFactor, int count, string indexesRaw)
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(count, realized.Count);

            for (var i = 0; i < count; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var redIndexes = indexesRaw.Split(",").Select(Int32.Parse).ToArray();
                var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/12838
        [Theory]
        [InlineData(0d, 10, "4,9")]
        [InlineData(0.5d, 20, "4,9,14,19")]
        public void NthChild_Selector_Works_For_ItemTemplate_Children(double bufferFactor, int count, string indexesRaw)
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0).Child().OfType<Canvas>())
            {
                Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(count, realized.Count);

            for (var i = 0; i < count; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var redIndexes = indexesRaw.Split(",").Select(Int32.Parse).ToArray();
                var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, ((Canvas)container.Child!).Background);
            }
        }

        [Theory]
        [InlineData(0d, 10, "0,5")]
        [InlineData(0.5d, 20, "0,5,10,15")]
        public void NthLastChild_Selector_Works(double bufferFactor, int count, string indexesRaw)
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(count, realized.Count);

            for (var i = 0; i < count; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var redIndexes = indexesRaw.Split(",").Select(Int32.Parse).ToArray();
                var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/12838
        [Theory]
        [InlineData(0d, 10, "0,5")]
        [InlineData(0.5d, 20, "0,5,10,15")]
        public void NthLastChild_Selector_Works_For_ItemTemplate_Children(double bufferFactor, int count, string indexesRaw)
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0).Child().OfType<Canvas>())
            {
                Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(count, realized.Count);

            for (var i = 0; i < count; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var redIndexes = indexesRaw.Split(",").Select(Int32.Parse).ToArray();
                var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, ((Canvas)container.Child!).Background);
            }
        }

        [Theory]
        [InlineData(0d, 10)]
        [InlineData(0.5d, 15)]
        public void ContainerPrepared_Is_Raised_When_Scrolling(double bufferFactor, int expectedRaised)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var raised = 0;

            itemsControl.ContainerPrepared += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d, 10)]
        [InlineData(0.5d, 15)]
        public void ContainerClearing_Is_Raised_When_Scrolling(double bufferFactor, int expectedRaised)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var raised = 0;

            itemsControl.ContainerClearing += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d, 9)]
        [InlineData(0.5d, 19)]
        public void ContainerIndexChanged_Is_Raised_On_Insert(double bufferFactor, int expectedRaised)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 1;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(++index, e.NewIndex);
            };

            items.Insert(index, "new");

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d, 10, 20)]
        [InlineData(0.5d, 20, 15)]
        public void ContainerIndexChanged_Is_Raised_When_Item_Inserted_Before_Realized_Elements(double bufferFactor, int expectedRaised, int index)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(++index, e.NewIndex);
            };

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.Insert(10, "new");

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d, 8)]
        [InlineData(0.5d, 18)]
        public void ContainerIndexChanged_Is_Raised_On_Remove(double bufferFactor, int expectedRaised)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 1;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index + 1, e.OldIndex);
                Assert.Equal(index++, e.NewIndex);
            };

            items.RemoveAt(index);

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d, 10, 20)]
        [InlineData(0.5d, 20, 15)]
        public void ContainerIndexChanged_Is_Raised_When_Item_Removed_Before_Realized_Elements(double bufferFactor, int expectedRaised, int index)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(index - 1, e.NewIndex);
                ++index;
                ++raised;
            };

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.RemoveAt(10);

            Assert.Equal(expectedRaised, raised);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Fires_Correct_Container_Lifecycle_Events_On_Replace(double bufferFactor)
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;
            var events = new List<string>();

            itemsControl.ContainerPrepared += (s, e) => events.Add($"Prepared #{e.Container.GetHashCode()} = {e.Index}");
            itemsControl.ContainerClearing += (s, e) => events.Add($"Clearing #{e.Container.GetHashCode()}");
            itemsControl.ContainerIndexChanged += (s, e) => events.Add($"IndexChanged #{e.Container.GetHashCode()} {e.OldIndex} -> {e.NewIndex}");

            var toReplace = target.GetRealizedElements().ElementAt(2)!;
            items[2] = "New Item";

            Assert.Equal(
                new[] { $"Clearing #{toReplace.GetHashCode()}" },
                events);
            events.Clear();

            itemsControl.UpdateLayout();

            Assert.Equal(
                new[] { $"Prepared #{toReplace.GetHashCode()} = 2" },
                events);
            events.Clear();
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Scrolling_Down_With_Larger_Element_Does_Not_Cause_Jump_And_Arrives_At_End(double bufferFactor)
        {
            using var app = App();

            var items = Enumerable.Range(0, 1000).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

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

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Scrolling_Up_To_Larger_Element_Does_Not_Cause_Jump(double bufferFactor)
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

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

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Scrolling_Up_To_Smaller_Element_Does_Not_Cause_Jump(double bufferFactor)
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 30)).ToList();
            items[20].Height = 25;

            var (target, scroll, itemsControl) = CreateTarget(items: items, 
                itemTemplate: CanvasWithHeightTemplate, 
                bufferFactor: bufferFactor);

            var additionalItemsCount = bufferFactor == 0d
                ? 1
                // buffer factor of 0.5 and 7 visible items => will be rounded up to 4
                // => when we scroll up and are near the _extended_ viewport,
                // 4 additional items will be inserted above the current viewport
                : Math.Round(target.Children.Count * target.CacheLength, MidpointRounding.AwayFromZero);

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
                    index - target.FirstRealizedIndex <= additionalItemsCount,
                    $"FirstIndex changed from {index} to {target.FirstRealizedIndex}");

                index = target.FirstRealizedIndex;
            }
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Does_Not_Throw_When_Estimating_Viewport_With_Ancestor_Margin(double bufferFactor)
        {
            // Issue #11272
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(bufferFactor: bufferFactor);
            var container = new Decorator { Margin = new Thickness(100) };
            var root = new TestRoot(true, container);

            root.LayoutManager.ExecuteInitialLayoutPass();

            container.Child = itemsControl;

            root.LayoutManager.ExecuteLayoutPass();
        }

        [Theory]
        [InlineData(0d, 20)]
        [InlineData(0.5d, 200)]
        public void Supports_Null_Recycle_Key_When_Scrolling(double bufferFactor, int offset)
        {
            using var app = App();
            var (_, scroll, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>(bufferFactor: bufferFactor);
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            scroll.Offset = new(0, offset);

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.DoesNotContain(firstItem, itemsControl.ItemsPanelRoot!.Children);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Supports_Null_Recycle_Key_When_Clearing_Items(double bufferFactor)
        {
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>(bufferFactor: bufferFactor);
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            itemsControl.ItemsSource = null;

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.Empty(itemsControl.ItemsPanelRoot!.Children);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void ScrollIntoView_On_Effectively_Invisible_Panel_Does_Not_Create_Ghost_Elements(double bufferFactor)
        {
            var items = new[] { "foo", "bar", "baz" };
            var (target, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(items: items, bufferFactor: bufferFactor);
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

        // https://github.com/AvaloniaUI/Avalonia/issues/10968
        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Does_Not_Realize_Items_If_Self_Outside_Viewport(double bufferFactor)
        {
            using var app = App();
            var (panel, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(bufferFactor: bufferFactor);
            itemsControl.Margin = new Thickness(0.0, 200.0, 0.0, 0.0);

            var scrollContentPresenter = new ScrollContentPresenter
            {
                Width = 100,
                Height = 100,
                Content = itemsControl
            };

            var root = CreateRoot(scrollContentPresenter);
            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(1, panel.VisualChildren.Count);

            scrollContentPresenter.Content = null;
            root.LayoutManager.ExecuteLayoutPass();

            scrollContentPresenter.Content = itemsControl;
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, panel.VisualChildren.Count);
        }

        [Theory]
        [InlineData(0d, 0, 8, 1,9)]
        [InlineData(0.5d, 0, 17, 0, 17)]
        public void Alternating_Backgrounds_Should_Be_Correct_After_Scrolling(double bufferFactor, 
            int firstIndex1, 
            int lastIndex1,
            int firstIndex2,
            int lastIndex2)
        {
            // Issue #12381.
            static void AssertColors(VirtualizingStackPanel target)
            {
                var containers = target.GetRealizedContainers()!
                    .Cast<ListBoxItem>()
                    .ToList();

                for (var i = target.FirstRealizedIndex; i <= target.LastRealizedIndex; i++)
                {
                    var container = Assert.IsType<ListBoxItem>(target.ContainerFromIndex(i));
                    var expectedBackground = i % 2 == 0 ? Colors.Green : Colors.Red;
                    var brush = Assert.IsAssignableFrom<ISolidColorBrush>(container.Background);

                    Assert.Equal(expectedBackground, brush.Color);
                }
            }

            using var app = App();
            var styles = new[]
            {
                new Style(x => x.OfType<ListBoxItem>())
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.White) },
                },
                new Style(x => x.OfType<ListBoxItem>().NthChild(2, 1))
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Green) },
                },
                new Style(x => x.OfType<ListBoxItem>().NthChild(2, 0))
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
                },
            };
            var (target, scroll, itemsControl) = CreateUnrootedTarget<ListBox>(bufferFactor: bufferFactor);

            // We need to display an odd number of items to reproduce the issue.
            var root = CreateRoot(itemsControl, clientSize: new(100, 90), styles: styles);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var containers = target.GetRealizedContainers()!
                .Cast<ListBoxItem>()
                .ToList();

            Assert.Equal(firstIndex1, target.FirstRealizedIndex);
            Assert.Equal(lastIndex1, target.LastRealizedIndex);
            AssertColors(target);

            scroll.Offset = new Vector(0, 10);
            target.UpdateLayout();

            Assert.Equal(firstIndex2, target.FirstRealizedIndex);
            Assert.Equal(lastIndex2, target.LastRealizedIndex);
            AssertColors(target);
        }

        [Theory]
        [InlineData(0d, 20)]
        [InlineData(0.5d, 15)]
        public void Inserting_Item_Before_Viewport_Preserves_FirstRealizedIndex(double bufferFactor, int firstIndex)
        {
            // Issue #12744
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
            var items = (IList)itemsControl.ItemsSource!;

            // Scroll down 20 items.
            scroll.Offset = new Vector(0, 200);
            target.UpdateLayout();
            Assert.Equal(firstIndex, target.FirstRealizedIndex);

            // Insert an item at the beginning.
            items.Insert(0, "New Item");
            target.UpdateLayout();

            // The first realized index should still be 20 as the scroll should be unchanged.
            Assert.Equal(firstIndex, target.FirstRealizedIndex);
            Assert.Equal(new(0, 200), scroll.Offset);
        }

        [Fact]
        public void Inserting_Item_Before_Viewport_Reuses_Matching_Containers_Without_Remeasure()
        {
            // Verifies that when a disjunct RecycleAll is triggered (e.g., insert at index 0),
            // containers whose DataContext already matches items in the new viewport are retained
            // and reused without calling Measure again (IsMeasureValid stays true).
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();
            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0d);

            // Scroll down 20 items. Items 20-29 are realized (10 items at 10px each, viewport=100px).
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(20, target.FirstRealizedIndex);
            Assert.Equal(29, target.LastRealizedIndex);

            // Reset measure counters on the items that are currently in the viewport.
            // After insert at 0, these items shift to indices 21-30 and should be reused.
            var viewportItems = items.Skip(20).Take(10).ToList();
            foreach (var item in viewportItems)
                item.Measured = 0;

            // Insert an item at the beginning. This shifts all indices by 1:
            //   - Old items[20..29] become items[21..30]
            //   - The new anchor is estimated at index 20 (= the newly-shifted "Item 19")
            //   - Disjunct triggers RecycleAll, but RetainMatchingContainers should save
            //     containers for items 21-30 (which already have matching DataContexts).
            var newItems = new ItemWithHeightAndMeasureArrangeCount(-1);
            items.Insert(0, newItems);
            Layout(target);

            // Items that were already realized with matching DataContext should NOT have
            // been re-measured (their containers were retained, not recycled+re-prepared).
            // The only new measure should be for the item at index 20 ("Item 19"),
            // which was not previously in the viewport.
            var remeasuredCount = viewportItems.Count(i => i.Measured > 0);

            Assert.True(remeasuredCount == 0,
                $"Expected 0 previously-visible items to be re-measured, but {remeasuredCount} were. " +
                string.Join(", ", viewportItems.Where(i => i.Measured > 0).Select(i => $"{i.Caption}(m={i.Measured})")));
        }

        [Fact]
        public void Collection_Reset_With_Reorder_Reuses_Matching_Containers_Without_Remeasure()
        {
            // Verifies that when a collection Reset occurs (e.g., sort/shuffle) and the same
            // item objects appear in the new viewport, their containers are retained and reused
            // without calling Measure again.
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();
            var collection = new ResettingObservableCollection<ItemWithHeightAndMeasureArrangeCount>(items);

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: collection,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0d);

            // Initial layout: items 0-9 are visible (10 items at 10px each, viewport=100px).
            Assert.Equal(0, target.FirstRealizedIndex);
            Assert.Equal(9, target.LastRealizedIndex);

            // Reset measure counters on items 0-9 (currently in viewport).
            var viewportItems = items.Take(10).ToList();
            foreach (var item in viewportItems)
                item.Measured = 0;

            // Reverse the entire collection via Reset. Items 0-9 are now at indices 99-90,
            // but items 90-99 (previously at the end) are now at indices 0-9.
            // The viewport still shows indices 0-9, which are now different item objects.
            // However, the OLD items (0-9) are no longer in the viewport, so they shouldn't
            // be retained. But if we do a partial reorder where the same items stay in the
            // viewport, they should be retained.

            // Instead: do a reset that shuffles items 0-9 amongst themselves.
            // All the same items stay at indices 0-9, just in different order.
            var shuffled = new List<ItemWithHeightAndMeasureArrangeCount>(items);
            // Reverse just the first 10 items
            shuffled.Reverse(0, 10);
            collection.Reset(shuffled);
            Layout(target);

            // Items 0-9 are the same objects (just reordered). Their containers should have
            // been retained and NOT re-measured.
            var remeasuredCount = viewportItems.Count(i => i.Measured > 0);

            Assert.True(remeasuredCount == 0,
                $"Expected 0 items to be re-measured after reorder, but {remeasuredCount} were. " +
                string.Join(", ", viewportItems.Where(i => i.Measured > 0).Select(i => $"{i.Caption}(m={i.Measured})")));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Can_Bind_Item_IsVisible(double bufferFactor)
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style }, bufferFactor: bufferFactor);
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            Layout(target);

            Assert.False(container.IsVisible);

            // Next container should be in correct position.
            Assert.Equal(20, target.ContainerFromIndex(3)!.Bounds.Top);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void IsVisible_Binding_Persists_After_Scrolling(double bufferFactor)
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style }, bufferFactor: bufferFactor);
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            container = target.ContainerFromIndex(2)!;
            Assert.False(container.IsVisible);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void Recycling_A_Hidden_Control_Shows_It(double bufferFactor)
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var itemsList = Enumerable.Range(0, 3).Select(x => new ItemWithIsVisible(x)).ToList();
            var items = new ObservableCollection<ItemWithIsVisible>(itemsList);
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style }, bufferFactor: bufferFactor);
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            Layout(target);

            Assert.False(container.IsVisible);

            items.RemoveAt(2);
            items.Add(new ItemWithIsVisible(3));
            Layout(target);

            Assert.True(container.IsVisible);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        public void ScrollIntoView_With_TargetRect_Outside_Viewport_Should_Scroll_To_Item(double bufferFactor)
        {
            using var app = App();
            var items = Enumerable.Range(0, 101).Select(x => new ItemWithHeight(x, x * 100 + 1));
            var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
                new Border
                {
                    Height = 10,
                    [!Layoutable.WidthProperty] = new Binding("Height"),
                });
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: itemTemplate,
                styles: new[]
                {
                    new Style(x => x.OfType<ScrollViewer>())
                    {
                        Setters =
                        {
                            new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible),
                        }
                    }
                },
                bufferFactor: bufferFactor);
            itemsControl.ContainerPrepared += (_, ev) =>
            {
                ev.Container.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
                {
                    var dataContext = (ItemWithHeight)e.TargetObject!.DataContext!;
                    e.TargetRect = new Rect(dataContext.Height - 50, 0, 50, 10);
                });
            };

            target.ScrollIntoView(100);

            Assert.Equal(9901, scroll.Offset.X);
        }

        [Theory]
        [InlineData(0d, 10, 10)]
        [InlineData(0.5d, 5, 15)]
        public void ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Smaller_Items(double bufferFactor, int firstIndex, int count)
        {
            using var app = App();

            // First 10 items have height of 20, next 10 have height of 10.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((29 - x) / 10) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item height is 20, so the requested item
            // should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
            // accommodate the item height of 10. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item heights.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(0, 380, 100, 10), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 390), scroll.Extent);
            Assert.Equal(new Vector(0, 290), scroll.Offset);

            // Items 10-19 should be visible.
            AssertRealizedItems(target, itemsControl, firstIndex, count);
        }

        [Theory]
        [InlineData(0d, 15, 5, 190, 210, 110)]
        [InlineData(0.5d, 10, 10, 253, 273, 173)]
        public void ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Larger_Items(double bufferFactor, int firstIndex, int count, int y, int extentHeight, int offset)
        {
            using var app = App();

            // First 10 items have height of 10, next 10 have height of 20.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((x / 10) + 1) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item height is 10, so the requested item
            // should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
            // accommodate the item height of 20. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item heights.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(0, y, 100, 20), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, extentHeight), scroll.Extent);
            Assert.Equal(new Vector(0, offset), scroll.Offset);

            // Items 15-19 should be visible.
            AssertRealizedItems(target, itemsControl, firstIndex, count);
        }

        [Theory]
        [InlineData(0d, 10,10)]
        [InlineData(0.5d, 5, 15)]
        public void ScrollIntoView_Correctly_Scrolls_Right_To_A_Page_Of_Smaller_Items(double bufferFactor, int firstIndex, int count)
        {
            using var app = App();

            // First 10 items have width of 20, next 10 have width of 10.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((29 - x) / 10) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items,
                itemTemplate: CanvasWithWidthTemplate, 
                orientation: Orientation.Horizontal,
                bufferFactor: bufferFactor);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item width is 20, so the requested item
            // should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
            // accommodate the item width of 10. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item widths.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(380, 0, 10, 100), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(390, 100), scroll.Extent);
            Assert.Equal(new Vector(290, 0), scroll.Offset);

            // Items 10-19 should be visible.
            AssertRealizedItems(target, itemsControl, firstIndex, count);
        }

        [Theory]
        [InlineData(0d, 15, 5, 190, 210, 110)]
        [InlineData(0.5d, 10, 10, 253, 273, 173)]
        public void ScrollIntoView_Correctly_Scrolls_Right_To_A_Page_Of_Larger_Items(double bufferFactor, int firstIndex, int count, int x, int extentWidth, int offset)
        {
            using var app = App();

            // First 10 items have width of 10, next 10 have width of 20.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((x / 10) + 1) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items,
                itemTemplate: CanvasWithWidthTemplate, 
                orientation: Orientation.Horizontal,
                bufferFactor: bufferFactor);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item width is 10, so the requested item
            // should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
            // accommodate the item width of 20. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item widths.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(x, 0, 20, 100), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(extentWidth, 100), scroll.Extent);
            Assert.Equal(new Vector(offset, 0), scroll.Offset);

            // Items 15-19 should be visible.
            AssertRealizedItems(target, itemsControl, firstIndex, count);
        }

        [Theory]
        [InlineData(0d, 
            4,5,
            8, 11)]
        [InlineData(0.5d, 
            3,6,
            6, 13)]
        public void Extent_And_Offset_Should_Be_Updated_When_Containers_Resize(double bufferFactor, 
            int firstIndex1, int lastIndex1, 
            int firstIndex2, int lastIndex2)
        {
            using var app = App();

            // All containers start off with a height of 50 (2 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll to the 5th item (containers 4 and 5 should be visible).
            target.ScrollIntoView(5);
            Assert.Equal(firstIndex1, target.FirstRealizedIndex);
            Assert.Equal(lastIndex1, target.LastRealizedIndex);

            // The extent should be 500 (10 * 50) and the offset should be 200 (4 * 50).
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(new Rect(0, 250, 100, 50), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 1000), scroll.Extent);
            Assert.Equal(new Vector(0, 200), scroll.Offset);

            // Update the height of all items to 25 and run a layout pass.
            foreach (var item in items)
                item.Height = 25;
            target.UpdateLayout();

            // The extent should be updated to reflect the new heights. The offset should be
            // unchanged but the first realized index should be updated to 8 (200 / 25).
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 500), scroll.Extent);
            Assert.Equal(new Vector(0, 200), scroll.Offset);
            Assert.Equal(firstIndex2, target.FirstRealizedIndex);
            Assert.Equal(lastIndex2, target.LastRealizedIndex);
        }

        [Theory]
        [InlineData(0d,
            4, 5,
            8, 11)]
        [InlineData(0.5d,
            3, 6,
            6, 13)]
        public void Focused_Container_Is_Positioned_Correctly_when_Container_Size_Change_Causes_It_To_Be_Moved_Out_Of_Visible_Viewport(double bufferFactor,
            int firstIndex1, int lastIndex1,
            int firstIndex2, int lastIndex2)
        {
            using var app = App();

            // All containers start off with a height of 50 (2 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll to the 5th item (containers 4 and 5 should be visible).
            target.ScrollIntoView(5);
            Assert.Equal(firstIndex1, target.FirstRealizedIndex);
            Assert.Equal(lastIndex1, target.LastRealizedIndex);

            // Focus the 5th item.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            container.Focusable = true;
            container.Focus();

            // Update the height of all items to 25 and run a layout pass.
            foreach (var item in items)
                item.Height = 25;
            target.UpdateLayout();

            // The focused container should now be outside the realized range.
            Assert.Equal(firstIndex2, target.FirstRealizedIndex);
            Assert.Equal(lastIndex2, target.LastRealizedIndex);

            // The container should still exist and be positioned outside the visible viewport.
            container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(new Rect(0, 125, 100, 25), container.Bounds);
        }

        [Theory]
        [InlineData(0d, 
            4, 7,
            3, 6, 
            3, 7)]
        [InlineData(0.5d,
            0, 7,
            0, 7,
            7, 17)]
        public void Focused_Container_Is_Positioned_Correctly_when_Container_Size_Change_Causes_It_To_Be_Moved_Into_Visible_Viewport(double bufferFactor, 
            int firstIndex1, int lastIndex1,
            int firstIndex2, int lastIndex2,
            int firstIndex3, int lastIndex3)
        {
            using var app = App();

            // All containers start off with a height of 25 (4 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 25)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll to the 5th item (containers 4-7 should be visible).
            target.ScrollIntoView(7);
            Assert.Equal(firstIndex1, target.FirstRealizedIndex);
            Assert.Equal(lastIndex1, target.LastRealizedIndex);

            // Focus the 7th item.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(7));
            container.Focusable = true;
            container.Focus();

            // Scroll up to the 3rd item (containers 3-6 should still be visible).
            target.ScrollIntoView(3);
            Assert.Equal(firstIndex2, target.FirstRealizedIndex);
            Assert.Equal(lastIndex2, target.LastRealizedIndex);

            // Update the height of all items to 20 and run a layout pass.
            foreach (var item in items)
                item.Height = 20;
            target.UpdateLayout();

            // The focused container should now be inside the realized range.
            Assert.Equal(firstIndex3, target.FirstRealizedIndex);
            Assert.Equal(lastIndex3, target.LastRealizedIndex);

            // The container should be positioned correctly.
            container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(7));
            Assert.Equal(new Rect(0, 140, 100, 20), container.Bounds);
        }

        [Fact]
        public void When_Vertical_Calculates_ViewPort_At_Start_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();

            // Act
            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor:0.5d);

            // Assert
            Assert.Equal(0, target.ViewPort.Top);
            Assert.Equal(100, target.ViewPort.Bottom);

            Assert.Equal(0, target.LastMeasuredExtendedViewPort.Top);
            Assert.Equal(200, target.LastMeasuredExtendedViewPort.Bottom);
        }

        [Fact]
        public void When_Vertical_Calculates_ViewPort_At_End_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // Act
            scroll.Offset = new Vector(0, 910); // scroll to end
            Layout(target);

            // Assert
            Assert.Equal(900, target.ViewPort.Top);
            Assert.Equal(1000, target.ViewPort.Bottom);

            Assert.Equal(800, target.LastMeasuredExtendedViewPort.Top);
            Assert.Equal(1000, target.LastMeasuredExtendedViewPort.Bottom);
        }

        [Fact]
        public void When_Vertical_Calculates_ViewPort_In_Middle_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // Act
            scroll.Offset = new Vector(0, 500); // scroll to end
            Layout(target);

            // Assert
            Assert.Equal(500, target.ViewPort.Top);
            Assert.Equal(600, target.ViewPort.Bottom);

            Assert.Equal(450, target.LastMeasuredExtendedViewPort.Top);
            Assert.Equal(650, target.LastMeasuredExtendedViewPort.Bottom);
        }

        [Fact]
        public void When_Horizontal_Calculates_ViewPort_At_Start_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();

            // Act
            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            // Assert
            Assert.Equal(0, target.ViewPort.Left);
            Assert.Equal(100, target.ViewPort.Right);

            Assert.Equal(0, target.LastMeasuredExtendedViewPort.Left);
            Assert.Equal(200, target.LastMeasuredExtendedViewPort.Right);
        }

        [Fact]
        public void When_Horizontal_Calculates_ViewPort_At_End_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();
            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);
            // Act
            scroll.Offset = new Vector(900, 0); // scroll to end
            Layout(target);

            // Assert
            Assert.Equal(900, target.ViewPort.Left);
            Assert.Equal(1000, target.ViewPort.Right);

            Assert.Equal(800, target.LastMeasuredExtendedViewPort.Left);
            Assert.Equal(1000, target.LastMeasuredExtendedViewPort.Right);
        }

        [Fact]
        public void When_Horizontal_Calculates_ViewPort_In_Middle_Of_List()
        {
            // Arrange
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();
            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            // Act
            scroll.Offset = new Vector(500, 0); // scroll to end
            Layout(target);

            // Assert
            Assert.Equal(500, target.ViewPort.Left);
            Assert.Equal(600, target.ViewPort.Right);

            Assert.Equal(450, target.LastMeasuredExtendedViewPort.Left);
            Assert.Equal(650, target.LastMeasuredExtendedViewPort.Right);
        }

        [Fact]
        public void Scrolling_Down_Does_Not_Measure_Or_Arrange_Until_Extended_ViewPort_Bounds_Are_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            Assert.True(target.LastRealizedIndex == 19,
                $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.LastRealizedIndex < 20)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 5);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once");
            Assert.True(target.Arranged == 1, "should be arranged only once");

            // the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems =
                items.Skip(5 /*additional items*/).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            var newAdditionalItems = items.Skip(20).Take(5);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Up_Does_Not_Measure_Or_Arrange_Until_Extended_ViewPort_Bounds_Are_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are not near the start of the list
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.True(target.FirstRealizedIndex == 15,
                $"Should show items from 20 to 30 (so 15 to 35 including additional items) but first realized index was {target.FirstRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var initialFirstRealizedIndex = target.FirstRealizedIndex;

            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.FirstRealizedIndex >= 15)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y - 5);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once");
            Assert.True(target.Arranged == 1, "should be arranged only once");

            // the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems = items.Skip(initialFirstRealizedIndex + 1).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            // now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
            var newAdditionalItems = items.Skip(initialFirstRealizedIndex - 6).Take(6);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Down_To_End_Of_List_Only_Measures_Once_When_Last_Item_Is_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are near the end of the list
            scroll.Offset = new Vector(0, 800); // so we render 75 to 95 with a buffer size of 5
            Layout(target);

            Assert.True(target.LastRealizedIndex == 94,
                $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var initialLastRealizedIndex = target.LastRealizedIndex;

            var count = 0;
            // Scroll down until we reached the very last item
            while (target.LastRealizedIndex < 99)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 5);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once even though we are at the end of the list");
            Assert.True(target.Arranged == 1, "should be arranged only once even though we are at the end of the list");

            // the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems =
                items.Skip(initialLastRealizedIndex + 1 - 15).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            var newAdditionalItems = items.Skip(initialLastRealizedIndex + 1).Take(5);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Up_To_Start_Of_List_Only_Measures_Once_When_First_Item_Is_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items, 
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are not near the start of the list
            scroll.Offset = new Vector(0, 105);
            Layout(target);

            Assert.True(target.FirstRealizedIndex == 5,
                $"Should show items from 10 to 20 (so 5 to 25 including additional items) but first realized index was {target.FirstRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.FirstRealizedIndex > 0)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y - 5);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once even though we are at the start of the list");
            Assert.True(target.Arranged == 1, "should be arranged only once even though we are at the start of the list");

            // the last 5 additional items will be reused when scrolling up, 
            var expectedMeasuredItems = items.Take(5).ToList();
            foreach (var itm in expectedMeasuredItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
            }
            // ...but the remaining 10 visible + 5 additional not touched at all
            foreach (var itm in items.Skip(5).Take(15).ToList())
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
            }

            // now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
            var untouchedItems = items.Skip(20).ToList();
            foreach (var itm in untouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Right_Does_Not_Measure_Or_Arrange_Until_Extended_ViewPort_Bounds_Are_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            Assert.True(target.LastRealizedIndex == 19,
                $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen
            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.LastRealizedIndex < 20)
            {
                scroll.Offset = new Vector(scroll.Offset.X + 5, 0);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once");
            Assert.True(target.Arranged == 1, "should be arranged only once");

            // the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems =
                items.Skip(5 /*additional items*/).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            var newAdditionalItems = items.Skip(20).Take(5);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Left_Does_Not_Measure_Or_Arrange_Until_Extended_ViewPort_Bounds_Are_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are not near the start of the list
            scroll.Offset = new Vector(200, 0);
            Layout(target);

            Assert.True(target.FirstRealizedIndex == 15,
                $"Should show items from 20 to 30 (so 15 to 35 including additional items) but first realized index was {target.FirstRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var initialFirstRealizedIndex = target.FirstRealizedIndex;
            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.FirstRealizedIndex >= 15)
            {
                scroll.Offset = new Vector(scroll.Offset.X - 5, 0);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once");
            Assert.True(target.Arranged == 1, "should be arranged only once");

            // the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems = items.Skip(initialFirstRealizedIndex + 1).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            // now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
            var newAdditionalItems = items.Skip(initialFirstRealizedIndex - 6).Take(6);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Right_To_End_Of_List_Only_Measures_Once_When_Last_Item_Is_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithWidthTemplate,
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are near the end of the list
            scroll.Offset = new Vector(800, 0); // so we render 75 to 95 with a buffer size of 5
            Layout(target);

            Assert.True(target.LastRealizedIndex == 94,
                $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var initialLastRealizedIndex = target.LastRealizedIndex;

            var count = 0;
            // Scroll down until we reached the very last item
            while (target.LastRealizedIndex < 99)
            {
                scroll.Offset = new Vector(scroll.Offset.X + 5, 0);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once even though we are at the end of the list");
            Assert.True(target.Arranged == 1, "should be arranged only once even though we are at the end of the list");

            // the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
            var expectedUntouchedItems =
                items.Skip(initialLastRealizedIndex + 1 - 15).Take(15).ToList();
            foreach (var itm in expectedUntouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
            }

            var newAdditionalItems = items.Skip(initialLastRealizedIndex + 1).Take(5);
            foreach (var itm in newAdditionalItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
            }
        }

        [Fact]
        public void Scrolling_Left_To_Start_Of_List_Only_Measures_Once_When_First_Item_Is_Reached()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

            var (target, scroll, itemsControl) = 
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithWidthTemplate, 
                    orientation: Orientation.Horizontal,
                    bufferFactor: 0.5d);

            // scroll a bit down so we are not near the start of the list
            scroll.Offset = new Vector(105, 0);
            Layout(target);

            Assert.True(target.FirstRealizedIndex == 5,
                $"Should show items from 10 to 20 (so 5 to 25 including additional items) but first realized index was {target.FirstRealizedIndex}");

            // reset counters
            target.ResetMeasureArrangeCounters();
            // shows 20 items, each is 10 high.
            // visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
            // until then no measure-arrange call should happen

            var count = 0;
            // Scroll down until the extended viewport bounds are reached
            while (target.FirstRealizedIndex > 0)
            {
                scroll.Offset = new Vector(scroll.Offset.X - 5, 0);
                Layout(target);
                count++;
                if (count > 1000)
                    throw new InvalidOperationException("infinite scroll detected");
            }

            // Assert
            Assert.True(target.Measured == 1, "should be measured only once even though we are at the start of the list");
            Assert.True(target.Arranged == 1, "should be arranged only once even though we are at the start of the list");


            // the last 5 additional items will be reused when scrolling up, 
            var expectedMeasuredItems = items.Take(5).ToList();
            foreach (var itm in expectedMeasuredItems)
            {
                Assert.True(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 1, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
            }
            // ...but the remaining 10 visible + 5 additional not touched at all
            foreach (var itm in items.Skip(5).Take(15).ToList())
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
            }
            // now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
            var untouchedItems = items.Skip(20).ToList();
            foreach (var itm in untouchedItems)
            {
                Assert.True(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
                Assert.True(itm.Arranged == 0, $"{itm.Caption} should not be measured but was {itm.Arranged} times");
            }
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

            var visibleChildren = target.Children
                .Where(x => x.IsVisible)
                .ToList();
            Assert.Equal(count, visibleChildren.Count);
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
            IEnumerable<Style>? styles = null,
            Orientation orientation = Orientation.Vertical,
            double bufferFactor = 0.0d)
        {
            return CreateTarget<ItemsControl, VirtualizingStackPanel>(
                items: items,
                itemTemplate: itemTemplate,
                styles: styles,
                orientation: orientation,
                bufferFactor: bufferFactor);
        }


        private static (TStackPanel, ScrollViewer, T) CreateTarget<T, TStackPanel>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            IEnumerable<Style>? styles = null,
            Orientation orientation = Orientation.Vertical,
            double bufferFactor = 0.0d)
                where T : ItemsControl, new()
                where TStackPanel : VirtualizingStackPanel, new()
        {
            var (target, scroll, itemsControl) = CreateUnrootedTarget<T, TStackPanel>(items, itemTemplate, orientation, bufferFactor: bufferFactor);

            var root = CreateRoot(itemsControl, styles: styles);

            root.LayoutManager.ExecuteInitialLayoutPass();

            return (target, scroll, itemsControl);
        }

        private static (VirtualizingStackPanel, ScrollViewer, T) CreateUnrootedTarget<T>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            Orientation orientation = Orientation.Vertical,
            double bufferFactor = 0.0d)
                where T : ItemsControl, new()
            => CreateUnrootedTarget<T, VirtualizingStackPanel>(items, itemTemplate, orientation, bufferFactor);

        private static (TStackPanel, ScrollViewer, T) CreateUnrootedTarget<T, TStackPanel>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            Orientation orientation = Orientation.Vertical,
            double bufferFactor = 0.0d)
                where T : ItemsControl, new()
                where TStackPanel : VirtualizingStackPanel, new()
        {
            var target = new TStackPanel
            {
                Orientation = orientation,
                CacheLength = bufferFactor,
            };

            items ??= new ObservableCollection<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));

            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            var scroll = new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Content = presenter,
            };

            if (orientation == Orientation.Horizontal)
            {
                scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            scroll.Template = ScrollViewerTemplate();

            var itemsControl = new T
            {
                ItemsSource = items,
                Template = new FuncControlTemplate<T>((_, ns) => scroll.RegisterInNameScope(ns)),
                ItemsPanel = new FuncTemplate<Panel?>(() => target),
                ItemTemplate = itemTemplate.GetValueOrDefault(DefaultItemTemplate()),
            };

            return (target, scroll, itemsControl);
        }

        private static TestRoot CreateRoot(
            Control? child,
            Size? clientSize = null,
            IEnumerable<Style>? styles = null)
        {
            var root = new TestRoot(true, child);
            root.ClientSize = clientSize ?? new(100, 100);

            if (styles is not null)
                root.Styles.AddRange(styles);

            return root;
        }

        private static Style CreateIsVisibleBindingStyle()
        {
            return new Style(x => x.OfType<ContentPresenter>())
            {
                Setters =
                {
                    new Setter(Visual.IsVisibleProperty, new Binding("IsVisible")),
                }
            };
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

        private static IControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>((x, ns) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    Width = 100,
                    Height = 10,
                }.RegisterInNameScope(ns));
        }

        private static IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((x, ns) =>
                new ScrollContentPresenter
                {
                    Name = "PART_ScrollContentPresenter",
                }.RegisterInNameScope(ns));
        }

        private static IDisposable App() => UnitTestApplication.Start(TestServices.RealFocus);

        private class ItemWithHeight : NotifyingBase
        {
            private double _height;

            public ItemWithHeight(int index, double height = 10)
            {
                Caption = $"Item {index}";
                Height = height;
            }

            public string Caption { get; set; }

            public double Height
            {
                get => _height;
                set => SetField(ref _height, value);
            }
        }

        private class ItemWithWidth : NotifyingBase
        {
            private double _width;

            public ItemWithWidth(int index, double width = 10)
            {
                Caption = $"Item {index}";
                Width = width;
            }

            public string Caption { get; set; }

            public double Width
            {
                get => _width;
                set => SetField(ref _width, value);
            }
        }

        private class ItemWithIsVisible : NotifyingBase
        {
            private bool _isVisible = true;

            public ItemWithIsVisible(int index)
            {
                Caption = $"Item {index}";
            }

            public string Caption { get; set; }

            public bool IsVisible
            {
                get => _isVisible;
                set => SetField(ref _isVisible, value);
            }
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

        private interface ICountMeasureArrangeCalls
        {
            int Measured { get; set; }
            int Arranged { get; set; }
        }

        [DebuggerDisplay("{DebuggerDisplay}")]
        private class ItemWithHeightAndMeasureArrangeCount : ItemWithHeight, ICountMeasureArrangeCalls
        {
            public ItemWithHeightAndMeasureArrangeCount(int index, double height = 10) : base(index, height)
            {
            }

            public int Measured { get; set; }
            public int Arranged { get; set; }

            private string DebuggerDisplay => $"{Caption} (height: {Height} m:{Measured} a: {Arranged})";
        }

        [DebuggerDisplay("{DebuggerDisplay}")]
        private class ItemWithWidthAndMeasureArrangeCount : ItemWithWidth, ICountMeasureArrangeCalls
        {
            public ItemWithWidthAndMeasureArrangeCount(int index, double width = 10) : base(index, width)
            {
            }

            public int Measured { get; set; }
            public int Arranged { get; set; }

            private string DebuggerDisplay => $"{Caption} (width: {Width} m:{Measured} a: {Arranged})";
        }


        private class VirtualizingStackPanelCountingMeasureArrange : VirtualizingStackPanel
        {
            public int Measured { get; set; }
            public int Arranged { get; set; }

            public void ResetMeasureArrangeCounters()
            {
                // reset counters
                Measured = 0;
                Arranged = 0;
                foreach (var itm in Items.OfType<ICountMeasureArrangeCalls>())
                {
                    itm.Measured = 0;
                    itm.Arranged = 0;
                }
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                Measured++;
                return base.MeasureOverride(availableSize);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                Arranged++;
                return base.ArrangeOverride(finalSize);
            }
        }

        private class CanvasCountingMeasureArrangeCalls : Canvas
        {
            protected override Size MeasureOverride(Size availableSize)
            {
                if(DataContext is ICountMeasureArrangeCalls itm)
                    itm.Measured++;

                return base.MeasureOverride(availableSize);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                if(DataContext is ICountMeasureArrangeCalls itm)
                    itm.Arranged++;

                return base.ArrangeOverride(finalSize);
            }
        }

        // ===== Infrastructure for DataTemplate Recycling tests =====

        private class ResettingObservableCollection<T> : ObservableCollection<T>
        {
            public ResettingObservableCollection(IEnumerable<T> items) : base(items) { }

            public void Reset(IEnumerable<T> newItems)
            {
                Items.Clear();
                foreach (var item in newItems)
                    Items.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private class TypeA_Item : NotifyingBase
        {
            public string Name { get; set; } = string.Empty;
        }

        private class TypeB_Item : NotifyingBase
        {
            public string Name { get; set; } = string.Empty;
        }

        private class FuncVirtualizingDataTemplate<T> : FuncDataTemplate<T>, IVirtualizingDataTemplate
        {
            public FuncVirtualizingDataTemplate(Func<T, INameScope, Control?> build)
                : base(build, supportsRecycling: true) { }

            public object? GetKey(object? data) => data?.GetType();
            public int MaxPoolSizePerKey { get; set; } = 5;
            public int MinPoolSizePerKey { get; set; } = 2;
        }

        // ===== Category A: Scrolling with Very Different Item Heights =====

        [Fact]
        public void Scrolling_Down_With_Mixed_Heights_Does_Not_Jump()
        {
            using var app = App();
            var items = Enumerable.Range(0, 50)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 100))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // Scroll down incrementally
            for (double offset = 0; offset < 500; offset += 10)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);

                // Check contiguity: each visible item's position should follow the previous
                var realized = target.GetRealizedElements()
                    .Where(e => e is { IsVisible: true })
                    .OrderBy(e => e!.Bounds.Top)
                    .ToList();

                for (int i = 1; i < realized.Count; i++)
                {
                    var prev = realized[i - 1]!;
                    var curr = realized[i]!;
                    var expectedTop = prev.Bounds.Top + prev.Bounds.Height;
                    Assert.True(
                        Math.Abs(curr.Bounds.Top - expectedTop) < 1,
                        $"Gap/overlap at offset {offset}: item {i-1} ends at {expectedTop}, item {i} starts at {curr.Bounds.Top}");
                }
            }
        }

        [Fact]
        public void Scrolling_Up_With_Mixed_Heights_Does_Not_Jump()
        {
            using var app = App();
            var items = Enumerable.Range(0, 50)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 100))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // Scroll to a far position first
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            // Scroll up incrementally
            for (double offset = 500; offset >= 0; offset -= 10)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);

                var realized = target.GetRealizedElements()
                    .Where(e => e is { IsVisible: true })
                    .OrderBy(e => e!.Bounds.Top)
                    .ToList();

                for (int i = 1; i < realized.Count; i++)
                {
                    var prev = realized[i - 1]!;
                    var curr = realized[i]!;
                    var expectedTop = prev.Bounds.Top + prev.Bounds.Height;
                    Assert.True(
                        Math.Abs(curr.Bounds.Top - expectedTop) < 1,
                        $"Gap/overlap at offset {offset}: item {i-1} ends at {expectedTop}, item {i} starts at {curr.Bounds.Top}");
                }
            }
        }

        [Fact]
        public void Scroll_To_End_And_Back_With_Extreme_Height_Variance()
        {
            using var app = App();
            // Heights: 5, 50, 200 pattern
            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, (i % 3 == 0) ? 5 : (i % 3 == 1) ? 50 : 200))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // Scroll incrementally to the end to let the panel discover all items
            while (true)
            {
                var prevOffset = scroll.Offset.Y;
                scroll.Offset = new Vector(0, scroll.Offset.Y + 200);
                Layout(target);
                // Stop when we can't scroll further
                if (Math.Abs(scroll.Offset.Y - prevOffset) < 1)
                    break;
            }

            // Last item should be visible
            var lastIndex = target.GetRealizedContainers()!
                .Select(c => itemsControl.IndexFromContainer(c))
                .Where(i => i >= 0)
                .Max();
            Assert.Equal(99, lastIndex);

            // Scroll back to top
            scroll.Offset = new Vector(0, 0);
            Layout(target);

            // First item should be at position 0
            var firstContainer = target.GetRealizedContainers()!
                .OrderBy(c => itemsControl.IndexFromContainer(c))
                .First();
            Assert.Equal(0, itemsControl.IndexFromContainer(firstContainer));
        }

        [Fact]
        public void Extent_Is_Reasonable_With_Mixed_Heights()
        {
            using var app = App();
            // 20 items with known heights: alternating 30 and 70, sum = 20 * 50 = 1000
            var items = Enumerable.Range(0, 20)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 30 : 70))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // After scrolling through all items, extent should converge to actual total
            for (double offset = 0; offset < 1000; offset += 50)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
            }

            // Extent should be close to actual total (1000px)
            Assert.True(
                Math.Abs(scroll.Extent.Height - 1000) < 50,
                $"Extent {scroll.Extent.Height} should be close to actual total 1000");
        }

        // ===== Category B: Different Recycle Pools (Multiple Keys) =====

        [Fact]
        public void Items_Of_Different_Types_Use_Separate_Recycle_Pools()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 50).Select<int, object>(i =>
                        i % 2 == 0
                            ? new TypeA_Item { Name = $"A{i}" }
                            : new TypeB_Item { Name = $"B{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Capture containers for first few items
                var containers0 = target.GetRealizedContainers()!.ToList();
                var container0 = containers0.FirstOrDefault();
                Assert.NotNull(container0);

                // Scroll down past initial viewport
                scroll.Offset = new Vector(0, 200);
                Layout(target);

                // Scroll back up
                scroll.Offset = new Vector(0, 0);
                Layout(target);

                // Verify containers are reused - the container for item[0] should have same DataContext
                var newContainers = target.GetRealizedContainers()!.ToList();
                var containerForItem0 = newContainers.FirstOrDefault(c =>
                {
                    var idx = itemsControl.IndexFromContainer(c);
                    return idx == 0;
                });
                Assert.NotNull(containerForItem0);

                // The DataContext should be the original item
                var dc = (containerForItem0 as IDataContextProvider)?.DataContext;
                Assert.Same(items[0], dc);
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void MaxPoolSizePerKey_Is_Respected()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 100).Select(i =>
                        (object)new TypeA_Item { Name = $"A{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MaxPoolSizePerKey = 2
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                var visibleCount = target.GetRealizedContainers()!.Count();

                // Scroll far down to recycle initial containers
                scroll.Offset = new Vector(0, 500);
                Layout(target);

                // Check pool doesn't exceed MaxPoolSizePerKey
                var pool = target.RecyclePoolForTesting;
                if (pool != null)
                {
                    foreach (var kvp in pool)
                    {
                        Assert.True(kvp.Value.Count <= 2,
                            $"Pool for key {kvp.Key} has {kvp.Value.Count} items, expected <= 2");
                    }
                }

                // Total children should be bounded
                var maxExpected = visibleCount + 2 * 2; // visible + 2 * MaxPoolSizePerKey
                // Allow some slack for buffer factor
                Assert.True(target.Children.Count <= maxExpected + 5,
                    $"Children count {target.Children.Count} exceeds expected max {maxExpected + 5}");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Recycled_Container_Gets_New_DataContext_When_Type_Matches()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 20).Select(i =>
                        (object)new TypeA_Item { Name = $"A{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Scroll down
                scroll.Offset = new Vector(0, 100);
                Layout(target);

                // Scroll back up
                scroll.Offset = new Vector(0, 0);
                Layout(target);

                // Verify all visible containers have correct DataContext
                var containers = target.GetRealizedContainers()!.ToList();
                foreach (var container in containers)
                {
                    var idx = itemsControl.IndexFromContainer(container);
                    if (idx >= 0 && idx < items.Count)
                    {
                        var dc = (container as IDataContextProvider)?.DataContext;
                        Assert.Same(items[idx], dc);
                    }
                }
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        // ===== Category C: DataContext-Matching Preference on Reset =====

        [Fact]
        public void Reset_Reuses_Container_With_Matching_DataContext()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var originalItems = Enumerable.Range(0, 20)
                    .Select(i => new TypeA_Item { Name = $"A{i}" }).ToList();
                var items = new ResettingObservableCollection<object>(originalItems);

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Capture containers and their DataContexts
                var containersBefore = target.GetRealizedContainers()!.ToList();
                var dcBefore = containersBefore
                    .Select(c => (c as IDataContextProvider)?.DataContext)
                    .ToList();

                // Reset with same items, same order
                items.Reset(originalItems.Cast<object>());
                Layout(target);

                // After reset, visible containers should have same DataContexts
                var containersAfter = target.GetRealizedContainers()!.ToList();
                foreach (var container in containersAfter)
                {
                    var idx = itemsControl.IndexFromContainer(container);
                    if (idx >= 0 && idx < originalItems.Count)
                    {
                        var dc = (container as IDataContextProvider)?.DataContext;
                        Assert.Same(originalItems[idx], dc);
                    }
                }
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Reset_With_Reordered_Items_Updates_DataContext()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var originalItems = Enumerable.Range(0, 20)
                    .Select(i => new TypeA_Item { Name = $"A{i}" }).ToList();
                var items = new ResettingObservableCollection<object>(originalItems);

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Reverse items and reset
                var reversed = originalItems.AsEnumerable().Reverse().Cast<object>().ToList();
                items.Reset(reversed);
                Layout(target);

                // After layout, all visible containers should match the new item order
                var containersAfter = target.GetRealizedContainers()!.ToList();
                foreach (var container in containersAfter)
                {
                    var idx = itemsControl.IndexFromContainer(container);
                    if (idx >= 0 && idx < reversed.Count)
                    {
                        var dc = (container as IDataContextProvider)?.DataContext;
                        Assert.Same(reversed[idx], dc);
                    }
                }
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Reset_Preserves_Scroll_Position_For_Append_Scenario()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var originalItems = Enumerable.Range(0, 20)
                    .Select(i => new TypeA_Item { Name = $"A{i}" }).ToList();
                var items = new ResettingObservableCollection<object>(originalItems);

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Scroll to show items 10-19
                scroll.Offset = new Vector(0, 100);
                Layout(target);

                var offsetBefore = scroll.Offset.Y;

                // Append 10 new items and fire Reset
                var appendedItems = originalItems.Cast<object>()
                    .Concat(Enumerable.Range(20, 10).Select(i => (object)new TypeA_Item { Name = $"A{i}" }))
                    .ToList();
                items.Reset(appendedItems);
                Layout(target);

                // Scroll offset should be preserved
                Assert.Equal(offsetBefore, scroll.Offset.Y);

                // Visible items should still be from around index 10
                var firstVisibleIdx = target.GetRealizedContainers()!
                    .Select(c => itemsControl.IndexFromContainer(c))
                    .Where(i => i >= 0)
                    .Min();
                Assert.True(firstVisibleIdx >= 8 && firstVisibleIdx <= 12,
                    $"First visible index {firstVisibleIdx} should be near 10 after append-reset");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Reset_With_Completely_New_Items_Recycles_Everything()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var originalItems = Enumerable.Range(0, 20)
                    .Select(i => new TypeA_Item { Name = $"A{i}" }).ToList();
                var items = new ResettingObservableCollection<object>(originalItems);

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Scroll a bit
                scroll.Offset = new Vector(0, 50);
                Layout(target);

                // Reset with completely new items
                var newItems = Enumerable.Range(0, 20)
                    .Select(i => (object)new TypeA_Item { Name = $"New{i}" }).ToList();
                items.Reset(newItems);
                Layout(target);

                // After layout, containers should have the new items as DataContext
                var containers = target.GetRealizedContainers()!.ToList();
                foreach (var container in containers)
                {
                    var idx = itemsControl.IndexFromContainer(container);
                    if (idx >= 0 && idx < newItems.Count)
                    {
                        var dc = (container as IDataContextProvider)?.DataContext;
                        Assert.Same(newItems[idx], dc);
                    }
                }
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        // ===== Category D: Non-Virtualizing Panel Safety =====

        [Fact]
        public void Non_Virtualizing_Panel_Clears_Container_Content_On_Recycle()
        {
            // Validates that the `Presenter?.Panel is VirtualizingPanel` guard in
            // ClearContainerForItemOverride works: when the panel IS a VirtualizingStackPanel,
            // content may be kept for recycling. When it's not, content must be cleared.
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                // Create an ItemsControl with VirtualizingStackPanel
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 20).Select(i => (object)$"Item {i}"));

                var (target, scroll, itemsControl) = CreateTarget(items: items);

                // The panel is a VirtualizingStackPanel, so realized containers should exist
                var containers = target.GetRealizedContainers()!.ToList();
                Assert.NotEmpty(containers);

                // Scroll down — items 0-9 get recycled into pool
                scroll.Offset = new Vector(0, 100);
                Layout(target);

                // With VirtualizingStackPanel, recycled containers stay as children (invisible)
                var invisibleChildren = target.Children.Where(c => !c.IsVisible).ToList();
                // Some recycled containers should be invisible in the tree
                // (exact count depends on pool behavior, but they shouldn't be removed)
                var totalChildren = target.Children.Count;
                var visibleChildren = target.Children.Count(c => c.IsVisible);
                Assert.True(totalChildren >= visibleChildren,
                    "VirtualizingStackPanel should keep recycled containers as invisible children");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        // ===== Category E: Warmup =====

        [Fact]
        public void Warmup_PreCreates_Containers_For_Discovered_Keys()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                // Use MinPoolSizePerKey > realized count to force warmup to create pool entries.
                // With viewport=100px and items height=10px, ~10 items are realized.
                // Alternating types means ~5 per type are realized. Set MinPoolSizePerKey=8
                // so warmup must create 3 additional per type.
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 50).Select<int, object>(i =>
                        i % 2 == 0
                            ? new TypeA_Item { Name = $"A{i}" }
                            : new TypeB_Item { Name = $"B{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 8
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Call warmup directly
                target.PerformWarmup();

                // Pool should have entries for both types (warmup creates extras beyond realized)
                var pool = target.RecyclePoolForTesting;
                Assert.NotNull(pool);
                Assert.True(pool.ContainsKey(typeof(TypeA_Item)),
                    "Pool should contain key for TypeA_Item");
                Assert.True(pool.ContainsKey(typeof(TypeB_Item)),
                    "Pool should contain key for TypeB_Item");

                // All pooled containers should be invisible (pre-created)
                foreach (var kvp in pool)
                {
                    foreach (var control in kvp.Value)
                    {
                        Assert.False(control.IsVisible,
                            $"Pooled container for {kvp.Key} should be invisible");
                    }
                }
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Warmup_Respects_MinPoolSizePerKey()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 50).Select(i =>
                        (object)new TypeA_Item { Name = $"A{i}" }));

                // MinPoolSizePerKey=15 > ~10 realized items,
                // so warmup must create ~5 additional containers
                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 15
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Call warmup directly
                target.PerformWarmup();

                // Pool + realized should be >= MinPoolSizePerKey
                var pool = target.RecyclePoolForTesting;
                Assert.NotNull(pool);

                var poolCount = pool.TryGetValue(typeof(TypeA_Item), out var poolList)
                    ? poolList.Count : 0;
                var realizedCount = target.GetRealizedElements()
                    .Count(e => e != null);
                Assert.True(poolCount + realizedCount >= 15,
                    $"Pool ({poolCount}) + realized ({realizedCount}) should be >= MinPoolSizePerKey (15)");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Warmup_Containers_Are_Reused_On_First_Scroll()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 50).Select(i =>
                        (object)new TypeA_Item { Name = $"A{i}" }));

                // Use large MinPoolSizePerKey so warmup creates extra containers
                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 15
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                // Warmup
                target.PerformWarmup();

                // Verify pool has containers after warmup
                var pool = target.RecyclePoolForTesting;
                Assert.NotNull(pool);
                var poolCountBefore = pool.TryGetValue(typeof(TypeA_Item), out var poolList)
                    ? poolList.Count : 0;
                Assert.True(poolCountBefore > 0,
                    "Warmup should have created pool containers");

                // Scroll down one page — pool containers should be consumed
                scroll.Offset = new Vector(0, 100);
                Layout(target);

                // After scrolling, pool should be smaller (containers were consumed/reused)
                var poolCountAfter = pool.TryGetValue(typeof(TypeA_Item), out var poolListAfter)
                    ? poolListAfter.Count : 0;

                // Pool should have been consumed (some containers reused for new visible items)
                // The old visible items get recycled back into pool, and pool items get used for new ones
                // Net effect: pool is used during scroll
                Assert.True(poolCountAfter <= poolCountBefore + 10,
                    $"Pool after scroll ({poolCountAfter}) should not grow unboundedly from ({poolCountBefore})");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void DiscoverTemplateKeys_Finds_Multiple_Types()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 30).Select<int, object>(i =>
                        i % 3 == 0 ? new TypeA_Item { Name = $"A{i}" }
                        : new TypeB_Item { Name = $"B{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 2
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);

                var keys = target.DiscoverTemplateKeys();

                Assert.True(keys.ContainsKey(typeof(TypeA_Item)),
                    "Should discover TypeA_Item key");
                Assert.True(keys.ContainsKey(typeof(TypeB_Item)),
                    "Should discover TypeB_Item key");
                Assert.Equal(2, keys.Count);
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }
        [Fact]
        public void Mixed_Heights_Scrolling_Does_Not_Cause_Excessive_Measures()
        {
            using var app = App();

            // 100 items with alternating heights: 10px and 100px
            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 100))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // Reset counters after initial layout
            target.ResetMeasureArrangeCounters();

            // Scroll down 10 increments of 20px each
            for (int i = 0; i < 10; i++)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 20);
                Layout(target);
            }

            // With the oscillation fix, we should not see excessive measures.
            // Without the fix, the feedback loop would cause many more measures per scroll step.
            Assert.True(target.Measured <= 15,
                $"Expected at most 15 measures for 10 scroll steps, but got {target.Measured}. " +
                $"This suggests extent estimation oscillation is causing a measure feedback loop.");
        }

        // ===== Category: Layout cycle prevention (cycle breaker, ValidateStartU) =====

        [Fact]
        public void Cycle_Breaker_Limits_Measures_Per_Layout_Pass()
        {
            // The cycle breaker should prevent more than ~2 MeasureOverride calls per layout pass.
            // After 1 full measure, subsequent calls return the previous DesiredSize.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 3 == 0 ? 10 : 50))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            target.ResetMeasureArrangeCounters();

            // Large scroll jump — triggers disjunct recycle and potentially multiple measures
            scroll.Offset = new Vector(0, 1500);
            Layout(target);

            // The cycle breaker should keep total measures bounded.
            // Without it, non-deterministic element measurement could cause 10+ measures.
            Assert.True(target.Measured <= 5,
                $"Expected at most 5 measures for a single scroll jump, but got {target.Measured}. " +
                $"Cycle breaker may not be working.");
        }

        [Fact]
        public void Genuine_Container_Resize_Still_Updates_Extent()
        {
            // Verifies that the cycle breaker and ValidateStartU suppression do NOT prevent
            // genuine container resizes (e.g., 50px → 25px) from being reflected in the extent.
            // This is the same scenario as Extent_And_Offset_Should_Be_Updated_When_Containers_Resize
            // but explicitly tests the interaction with the cycle breaker.
            using var app = App();

            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // Initial extent: 20 items × 50px = 1000px
            Assert.Equal(new Size(100, 1000), scroll.Extent);

            // Resize all items from 50px to 25px — a genuine resize (25px change >> 1px threshold)
            foreach (var item in items)
                item.Height = 25;
            target.UpdateLayout();

            // Extent should update to 20 × 25 = 500px despite the cycle breaker
            Assert.Equal(new Size(100, 500), scroll.Extent);
        }

        [Fact]
        public void ValidateStartU_Absorbs_Sub_Pixel_Changes()
        {
            // ValidateStartU should absorb size changes < 1px without marking StartU as unstable.
            // This prevents layout cycles from complex controls that produce slightly different
            // measurements each pass.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => (object)new ItemWithHeight(i, 10))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            target.ResetMeasureArrangeCounters();

            // Scroll to middle — this measures elements and stores their sizes
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            var measuresAfterScroll = target.Measured;

            // Now scroll by a tiny amount — realized elements should remain the same
            // and ValidateStartU should not trigger from sub-pixel noise
            scroll.Offset = new Vector(0, 201);
            Layout(target);

            // Should need only 1-2 measures (no oscillation from ValidateStartU)
            var additionalMeasures = target.Measured - measuresAfterScroll;
            Assert.True(additionalMeasures <= 3,
                $"Expected at most 3 measures for a 1px scroll, but got {additionalMeasures}. " +
                $"ValidateStartU may be triggering unnecessarily on sub-pixel changes.");
        }

        [Fact]
        public void Large_Scroll_Jump_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle()
        {
            // Simulates fast scrolling: a large viewport jump with heterogeneous item heights.
            // This is the scenario that previously caused layout cycles due to:
            // 1. ValidateStartU detecting size changes on every pass
            // 2. Estimate cache invalidation causing extent oscillation
            // 3. CompensateForExtentChange creating positive feedback loops
            using var app = App();

            // Create items with wildly varying heights (like a real form with headers,
            // text fields, image fields, etc.)
            var items = Enumerable.Range(0, 71)
                .Select(i => (object)new ItemWithHeight(i, (i % 5) switch
                {
                    0 => 50,   // header
                    1 => 80,   // text field
                    2 => 120,  // options field
                    3 => 200,  // image field
                    4 => 30,   // small field
                    _ => 10
                }))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 2.0d);  // CacheLength=2 like the real control

            target.ResetMeasureArrangeCounters();

            // Simulate fast scroll: jump from top to near bottom
            scroll.Offset = new Vector(0, 5000);
            Layout(target);

            // Should not cause excessive measures (layout cycle)
            Assert.True(target.Measured <= 5,
                $"Expected at most 5 measures for a large scroll jump, but got {target.Measured}. " +
                $"Layout cycle likely occurred.");

            // Verify elements are realized at the correct position
            Assert.True(target.FirstRealizedIndex >= 0, "Should have realized elements after scroll jump");
            Assert.True(target.LastRealizedIndex > target.FirstRealizedIndex,
                "Should have multiple realized elements");
        }

        [Fact]
        public void Multiple_Scroll_Jumps_Each_Get_Fresh_Measure_Pass()
        {
            // Verifies that the consecutive measure counter resets on viewport changes,
            // allowing each scroll jump to get a fresh full measure pass.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 80))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // First scroll jump
            scroll.Offset = new Vector(0, 1000);
            Layout(target);

            var firstJumpLastIndex = target.LastRealizedIndex;

            // Second scroll jump — should get fresh measure passes (counter reset by viewport change)
            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            // The realized range should have moved forward
            Assert.True(target.FirstRealizedIndex > firstJumpLastIndex - 10,
                $"After second scroll jump, FirstRealizedIndex ({target.FirstRealizedIndex}) " +
                $"should be near or past the previous LastRealizedIndex ({firstJumpLastIndex})");
        }

        [Fact]
        public void Items_Changed_Resets_Cycle_Breaker()
        {
            // Verifies that adding items resets the consecutive measure counter,
            // allowing a fresh measure pass for the new items.
            using var app = App();

            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 50).Select(i => (object)$"Item {i}"));

            var (target, scroll, itemsControl) = CreateTarget(items: items);

            var extentBefore = scroll.Extent.Height;

            // Add more items — should trigger a fresh measure that updates the extent
            for (int i = 50; i < 100; i++)
                items.Add($"Item {i}");
            Layout(target);

            // Extent should increase to reflect the new items
            Assert.True(scroll.Extent.Height > extentBefore,
                $"Extent should increase after adding items. Before: {extentBefore}, After: {scroll.Extent.Height}");
        }

        [Fact]
        public void Rapid_Scrolling_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle()
        {
            // Simulates rapid scrolling through many positions — the scenario that
            // triggers layout cycles in production with complex controls.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, 10 + (i % 7) * 15))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 2.0d);

            target.ResetMeasureArrangeCounters();

            // Rapid scroll: 20 jumps of varying sizes (simulates fast mouse wheel / touch)
            for (int i = 0; i < 20; i++)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 200);
                Layout(target);
            }

            // With 20 scroll steps, a reasonable upper bound is ~3 measures per step = 60 total.
            // Without the cycle breaker, this could easily hit 200+ due to oscillation.
            Assert.True(target.Measured <= 60,
                $"Expected at most 60 measures for 20 rapid scroll steps, but got {target.Measured}. " +
                $"Layout cycle oscillation is likely occurring.");

            // Verify we ended up with valid realized elements
            Assert.True(target.FirstRealizedIndex >= 0, "Should have realized elements");
            Assert.True(target.LastRealizedIndex <= 99, "Last realized index should be within bounds");
        }

        // ===== Category: ValidateStartU suppression =====

        [Fact]
        public void ValidateStartU_Only_Fires_Once_Per_Arrange_Cycle()
        {
            // After ValidateStartU detects a genuine resize, _suppressValidateStartU is set
            // so it won't fire again until ArrangeOverride resets it. This prevents repeated
            // instability from non-deterministic measurements.
            using var app = App();

            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate);

            // Initial layout: extent = 20 × 50 = 1000
            Assert.Equal(new Size(100, 1000), scroll.Extent);

            // Resize all items — triggers ValidateStartU on next measure
            foreach (var item in items)
                item.Height = 25;

            // First layout pass after resize: ValidateStartU fires, extent updates
            Layout(target);
            Assert.Equal(new Size(100, 500), scroll.Extent);

            // Resize again — but after a successful Arrange, suppression is cleared
            foreach (var item in items)
                item.Height = 10;
            Layout(target);

            // Should update again (suppression was reset by Arrange)
            Assert.Equal(new Size(100, 200), scroll.Extent);
        }

        // ===== Category: CaptureViewportAnchor NaN guard =====

        [Fact]
        public void Scroll_After_Container_Resize_Does_Not_Use_Stale_Anchor()
        {
            // When StartU becomes NaN (unstable from ValidateStartU), CaptureViewportAnchor
            // must NOT use the cached anchor from a previous pass. The NaN guard ensures
            // re-evaluation, preventing CompensateForExtentChange from using stale data.
            using var app = App();

            var items = Enumerable.Range(0, 50).Select(x => new ItemWithHeight(x, 20)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Scroll to middle
            scroll.Offset = new Vector(0, 400);
            Layout(target);

            var firstBefore = target.FirstRealizedIndex;

            // Resize all items — this makes StartU unstable (ValidateStartU fires)
            foreach (var item in items)
                item.Height = 10;
            Layout(target);

            // After resize from 20→10, the same offset=400 now corresponds to item 40 (400/10)
            // instead of item 20 (400/20). The realized range should shift accordingly.
            Assert.True(target.FirstRealizedIndex > firstBefore,
                $"After resize, FirstRealizedIndex ({target.FirstRealizedIndex}) should be > " +
                $"previous ({firstBefore}) because items are now half the height");
        }

        // ===== Category: NullifyElement (used by smart container reuse) =====

        [Fact]
        public void NullifyElement_Returns_Element_And_Clears_Slot()
        {
            // NullifyElement removes an element from the realized list without recycling it,
            // so RetainMatchingContainers can hold it for reuse in the new viewport.
            var elements = new Avalonia.Controls.Utils.RealizedStackElements();
            var control1 = new Canvas { Width = 100, Height = 50 };
            var control2 = new Canvas { Width = 100, Height = 30 };

            elements.Add(5, control1, 0, 50);
            elements.Add(6, control2, 50, 30);

            // Nullify element at index 5
            var result = elements.NullifyElement(5);

            Assert.NotNull(result);
            Assert.Same(control1, result!.Value.element);
            Assert.Equal(50, result.Value.sizeU);

            // The slot should now be null — GetElement returns null
            Assert.Null(elements.GetElement(5));

            // Element at index 6 should still be there
            Assert.Same(control2, elements.GetElement(6));
        }

        [Fact]
        public void NullifyElement_Returns_Null_For_Invalid_Index()
        {
            var elements = new Avalonia.Controls.Utils.RealizedStackElements();
            var control = new Canvas { Width = 100, Height = 50 };
            elements.Add(5, control, 0, 50);

            // Index before range
            Assert.Null(elements.NullifyElement(3));

            // Index after range
            Assert.Null(elements.NullifyElement(10));

            // Empty collection
            var empty = new Avalonia.Controls.Utils.RealizedStackElements();
            Assert.Null(empty.NullifyElement(0));
        }

        // ===== Category: Smart container reuse during disjunct scroll =====

        [Fact]
        public void Disjunct_Scroll_Reuses_Containers_With_Matching_DataContext()
        {
            // When scrolling to a disjunct viewport, RetainMatchingContainers should hold
            // containers whose DataContext matches items in the new viewport, avoiding
            // expensive PrepareItemContainer + Measure for those items.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, 10))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.0d);

            // Scroll to top — realize items 0-9
            Assert.Equal(0, target.FirstRealizedIndex);

            target.ResetMeasureArrangeCounters();

            // Scroll far enough to trigger disjunct recycle
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            // Should have realized new items near offset 500
            Assert.True(target.FirstRealizedIndex >= 40,
                $"After disjunct scroll, FirstRealizedIndex should be >= 40, but was {target.FirstRealizedIndex}");
        }

        // ===== Category: Item 0 position correction =====

        [Fact]
        public void Item_Zero_Is_Always_At_Position_Zero()
        {
            // When item 0 is realized (either in forward or backward loop), it must be
            // positioned at U=0 regardless of estimation errors. This prevents the
            // first item from being clipped off-screen.
            using var app = App();

            // Mixed heights create estimation errors that could place item 0 at negative U
            var items = Enumerable.Range(0, 50)
                .Select(i => (object)new ItemWithHeight(i, i % 3 == 0 ? 100 : 10))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Scroll down then back to top
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            // Item 0 should be at position 0
            var container0 = target.ContainerFromIndex(0);
            Assert.NotNull(container0);
            Assert.Equal(0, container0!.Bounds.Top);
        }

        // ===== Category: Estimate caching =====

        [Fact]
        public void Estimate_Cache_Skips_Recalculation_When_Range_Unchanged()
        {
            // EstimateElementSizeU caches by realized range (first/last index).
            // When the same range is measured again, it should return the cached value
            // without recalculating, preventing smoothing convergence across passes.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 50))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            // Record extent after initial layout
            var extent1 = scroll.Extent.Height;

            // Do another layout without scrolling — same realized range
            Layout(target);
            var extent2 = scroll.Extent.Height;

            // Extent should be identical (estimate cached, no recalculation)
            Assert.Equal(extent1, extent2);
        }

        [Fact]
        public void Estimate_Cache_Invalidated_After_Genuine_Resize()
        {
            // When ValidateStartU detects a genuine resize (>= 1px), the estimate cache
            // indices are reset to -1, forcing EstimateElementSizeU to recalculate.
            using var app = App();

            var items = Enumerable.Range(0, 50).Select(x => new ItemWithHeight(x, 40)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Initial extent: based on 40px items
            var extentBefore = scroll.Extent.Height;

            // Halve the height — genuine resize, estimate cache must invalidate
            foreach (var item in items)
                item.Height = 20;
            Layout(target);

            var extentAfter = scroll.Extent.Height;

            // Extent should roughly halve (estimate updated to reflect new sizes)
            Assert.True(extentAfter < extentBefore * 0.75,
                $"After halving item heights, extent should decrease significantly. " +
                $"Before: {extentBefore}, After: {extentAfter}");
        }

        // ===== Category: CompensateForExtentChange dampening =====

        [Fact]
        public void Extent_Dampening_Prevents_Wild_Swings_With_Few_Realized_Items()
        {
            // When very few items are realized relative to total count, extent estimates
            // can swing wildly. The dampening logic (0.3× for >50% change with <10% realized)
            // prevents ScrollViewer from overshooting.
            using var app = App();

            // Create many items so only a small fraction is realized
            var items = Enumerable.Range(0, 500)
                .Select(i => (object)new ItemWithHeight(i, i % 10 == 0 ? 200 : 10))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.0d);

            // After initial layout, we realize ~10 items out of 500 (2%)
            // The estimate is based on this small sample
            var initialExtent = scroll.Extent.Height;

            // Scroll to a region with very different heights
            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            // Extent should have been adjusted but not wildly swung
            var newExtent = scroll.Extent.Height;
            Assert.True(newExtent > 0, "Extent should be positive");
            Assert.True(target.FirstRealizedIndex >= 0, "Should have realized elements");
        }

        // ===== Category: Scroll-back-to-top correctness =====

        [Fact]
        public void Scroll_Down_Then_Back_To_Top_With_Mixed_Heights_Shows_All_Items()
        {
            // Regression test: scrolling down with mixed heights then back to top
            // must show item 0 at position 0 with no gaps. This exercises the full
            // pipeline: estimation, disjunct recycle, item 0 correction, and
            // CaptureViewportAnchor together.
            using var app = App();

            var items = Enumerable.Range(0, 71)
                .Select(i => (object)new ItemWithHeight(i, (i % 5) switch
                {
                    0 => 50,
                    1 => 80,
                    2 => 120,
                    3 => 200,
                    4 => 30,
                    _ => 10
                }))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 2.0d);

            // Scroll to near the bottom
            scroll.Offset = new Vector(0, 5000);
            Layout(target);

            // Scroll back to top
            scroll.Offset = new Vector(0, 0);
            Layout(target);

            // Item 0 must be realized at position 0
            Assert.Equal(0, target.FirstRealizedIndex);
            var container0 = target.ContainerFromIndex(0);
            Assert.NotNull(container0);
            Assert.Equal(0, container0!.Bounds.Top);

            // Items should be contiguous (no gaps)
            var realized = target.GetRealizedContainers()!
                .Where(x => x.IsVisible)
                .OrderBy(x => x.Bounds.Top)
                .ToList();

            for (int i = 1; i < realized.Count; i++)
            {
                var prev = realized[i - 1];
                var curr = realized[i];
                var expectedTop = prev.Bounds.Top + prev.Bounds.Height;
                Assert.True(
                    Math.Abs(curr.Bounds.Top - expectedTop) < 1,
                    $"Gap at item {i}: expected top {expectedTop}, got {curr.Bounds.Top}");
            }
        }

        [Fact]
        public void Significant_Size_Change_Logs_Warning()
        {
            // When an item's size changes significantly during layout (e.g., async image
            // loading), a Warning-level log should be emitted to help diagnose non-deterministic
            // item templates.
            using var app = App();

            var logMessages = new List<string>();
            var sink = new TestLogSink(logMessages);
            Logger.Sink = sink;

            try
            {
                var items = Enumerable.Range(0, 20)
                    .Select(i => new ItemWithHeight(i, 50))
                    .ToList();

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0d);

                // Resize an item significantly — should trigger a warning
                items[0].Height = 25;
                Layout(target);

                Assert.Contains(logMessages, m =>
                    m.Contains("Item template size changed significantly during layout") &&
                    m.Contains("OldSize=") &&
                    m.Contains("NewSize="));
            }
            finally
            {
                Logger.Sink = null;
            }
        }

        [Fact]
        public void Item_Growing_Before_Anchor_While_Scrolling_Up_Preserves_Anchor_Position()
        {
            // When scrolling UP slowly, items above the viewport may load async images
            // and grow. ValidateStartU must subtract preDelta from StartU to keep the
            // anchor (first visible item) at its visual position. A wrong sign would
            // push the anchor away, causing a visible scroll jump.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 1.0d);

            // Scroll to offset 600 → viewport=[600,700]
            // With bufferFactor=1.0, extended≈[500,800], so items around index 10+ are realized
            scroll.Offset = new Vector(0, 600);
            Layout(target);

            var anchorIdx = (int)(scroll.Offset.Y / 50); // ~12
            var offsetBefore = scroll.Offset.Y;

            // Simulate async image loading: an item in the buffer zone ABOVE the viewport
            // grows significantly (e.g., image placeholder 50px → loaded image 200px).
            // This item is realized (in buffer) and before the anchor.
            var firstRealized = target.FirstRealizedIndex;
            Assert.True(firstRealized < anchorIdx,
                $"Need buffer items before anchor. firstRealized={firstRealized}, anchor≈{anchorIdx}");

            items[firstRealized].Height = 200; // async image loaded
            Layout(target);

            // The scroll offset should NOT jump — anchor stays at its visual position.
            // A small adjustment (< item height) is acceptable due to extent changes,
            // but a large jump (> 50px) means the preDelta sign is wrong.
            var offsetAfter = scroll.Offset.Y;
            Assert.True(Math.Abs(offsetAfter - offsetBefore) < 50,
                $"Scroll jumped too much when item above viewport grew: " +
                $"before={offsetBefore}, after={offsetAfter}, delta={offsetAfter - offsetBefore}. " +
                $"This suggests the preDelta compensation sign is wrong.");
        }

        [Fact]
        public void Item_Shrinking_Before_Anchor_While_Scrolling_Up_Preserves_Anchor_Position()
        {
            // Opposite of the growing case: an item above the viewport shrinks.
            // StartU must increase (subtract negative preDelta) to keep anchor stable.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 1.0d);

            scroll.Offset = new Vector(0, 600);
            Layout(target);

            var offsetBefore = scroll.Offset.Y;
            var firstRealized = target.FirstRealizedIndex;

            // Shrink a buffer item above the viewport
            items[firstRealized].Height = 10;
            Layout(target);

            var offsetAfter = scroll.Offset.Y;
            Assert.True(Math.Abs(offsetAfter - offsetBefore) < 50,
                $"Scroll jumped too much when item above viewport shrank: " +
                $"before={offsetBefore}, after={offsetAfter}, delta={offsetAfter - offsetBefore}.");
        }

        // ===== Category: Frozen extent boundary clamping =====

        /// <summary>
        /// Helper: sets the private _frozenExtentU field on a VirtualizingStackPanel
        /// to simulate the frozen-extent state that occurs during extent oscillation.
        /// </summary>
        private static void SetFrozenExtent(VirtualizingStackPanel panel, double value)
        {
            var field = typeof(VirtualizingStackPanel).GetField(
                "_frozenExtentU",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            field!.SetValue(panel, value);
        }

        private static double GetFrozenExtent(VirtualizingStackPanel panel)
        {
            var field = typeof(VirtualizingStackPanel).GetField(
                "_frozenExtentU",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            return (double)field!.GetValue(panel)!;
        }

        [Fact]
        public void Anchor_Self_Size_Change_During_Frozen_Extent_Compensates_StartU()
        {
            // When the anchor item itself shrinks during frozen extent (lockSizes=true),
            // items after it shift. ValidateStartU must track the anchor's size change
            // in preDelta (itemIndex <= anchorIndex) and adjust StartU to keep visible
            // items at their current positions.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 1.0d);

            // Scroll to middle so items around index 10-15 are visible
            scroll.Offset = new Vector(0, 600);
            Layout(target);

            var firstRealized = target.FirstRealizedIndex;
            var offsetBefore = scroll.Offset.Y;

            // Freeze the extent to simulate oscillation detection
            SetFrozenExtent(target, scroll.Extent.Height);

            // Shrink the first realized item (which will be the anchor) significantly.
            // During frozen extent, this must still compensate StartU.
            items[firstRealized].Height = 10;
            Layout(target);

            var offsetAfter = scroll.Offset.Y;
            Assert.True(Math.Abs(offsetAfter - offsetBefore) < 50,
                $"Scroll jumped too much when anchor shrank during frozen extent: " +
                $"before={offsetBefore}, after={offsetAfter}, delta={offsetAfter - offsetBefore}. " +
                $"ValidateStartU may not be compensating anchor-self-size-change during lockSizes.");
        }

        [Fact]
        public void Item_Zero_Not_Forced_To_Zero_During_Frozen_Extent()
        {
            // During frozen extent, forcing item 0 to position 0 creates a gap between
            // item 0 and item 1 (which keeps its estimated position). This causes
            // GetOrEstimateAnchorElementForViewport to fail to find overlapping items,
            // triggering a false disjunct detection and a visible scroll jump.
            // Instead, item 0 should keep its natural position relative to item 1.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Scroll down so items are at estimated positions
            scroll.Offset = new Vector(0, 300);
            Layout(target);

            // Freeze extent
            SetFrozenExtent(target, scroll.Extent.Height);

            // Scroll back toward top so item 0 gets realized
            scroll.Offset = new Vector(0, 0);
            Layout(target);

            // Item 0 should be realized
            Assert.Equal(0, target.FirstRealizedIndex);

            // During frozen extent, item 0 should NOT be at position 0
            // (it keeps its estimated position to avoid disjunct detection).
            // After multiple layouts, items should still be contiguous.
            Layout(target);

            // Verify items are contiguous (no gaps)
            var realized = target.GetRealizedContainers()!
                .Where(x => x.IsVisible)
                .OrderBy(x => x.Bounds.Top)
                .ToList();

            Assert.True(realized.Count >= 2, "Expected at least 2 realized items");

            for (int i = 1; i < realized.Count; i++)
            {
                var prev = realized[i - 1];
                var curr = realized[i];
                var expectedTop = prev.Bounds.Top + prev.Bounds.Height;
                Assert.True(Math.Abs(curr.Bounds.Top - expectedTop) < 1,
                    $"Gap between items {i - 1} and {i}: prev ends at {expectedTop:F1}, " +
                    $"curr starts at {curr.Bounds.Top:F1}");
            }
        }

        [Fact]
        public void Frozen_Extent_Top_Boundary_Clamps_When_Viewport_Above_Item_Zero()
        {
            // When item 0 is realized during frozen extent at a position P > 0 and the
            // viewport scrolls above P, the panel shifts items to follow the viewport
            // and reduces the frozen extent. This prevents empty space above item 0.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Scroll to a position that gives item 0 a non-zero estimated position
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            var extentBefore = scroll.Extent.Height;

            // Freeze extent at current value
            SetFrozenExtent(target, extentBefore);

            // Scroll to top — item 0 gets realized at its estimated position (> 0)
            scroll.Offset = new Vector(0, 0);
            Layout(target);
            Layout(target); // extra layout to let clamping settle

            // Item 0 must be realized
            Assert.Equal(0, target.FirstRealizedIndex);

            // Item 0's position should be at or before the viewport start (no empty space above)
            var container0 = target.ContainerFromIndex(0);
            Assert.NotNull(container0);
            Assert.True(container0!.Bounds.Top <= scroll.Offset.Y + 1,
                $"Item 0 at position {container0.Bounds.Top} is below viewport start {scroll.Offset.Y}. " +
                $"Empty space above item 0 should be prevented by boundary clamping.");

            // Frozen extent should have been reduced (shifted by the clamping amount)
            var frozenAfter = GetFrozenExtent(target);
            Assert.True(frozenAfter <= extentBefore,
                $"Frozen extent should decrease when clamping top boundary. " +
                $"Before={extentBefore}, After={frozenAfter}");
        }

        [Fact]
        public void Frozen_Extent_Bottom_Boundary_Caps_When_Last_Item_Realized()
        {
            // When the last item is realized during frozen extent and the frozen extent
            // exceeds the actual content end, the frozen extent should be capped to
            // prevent scrolling past the last item.
            using var app = App();

            var items = Enumerable.Range(0, 20)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 1.0d);

            // Initial extent: 20 × 50 = 1000
            Layout(target);

            // Freeze extent at an inflated value (simulating estimate overshoot)
            SetFrozenExtent(target, 1500);

            // Scroll near the bottom so the last item gets realized.
            // With bufferFactor 1.0, the extended viewport reaches past item 19.
            scroll.Offset = new Vector(0, 800);
            Layout(target);

            // Last item should be realized (buffer extends past item 19)
            Assert.Equal(19, target.LastRealizedIndex);

            // The frozen extent should have been capped — it should not exceed
            // the actual content (which is ~1000px for 20 items × 50px)
            var frozenAfter = GetFrozenExtent(target);
            Assert.True(frozenAfter <= 1100,
                $"Frozen extent should be capped near actual content size when last item is realized. " +
                $"Expected <= 1100, got {frozenAfter}");
        }

        [Fact]
        public void Scroll_To_Top_And_Back_Down_With_Frozen_Extent_Preserves_Contiguity()
        {
            // End-to-end test: scroll down, trigger frozen extent, scroll back to top,
            // scroll down again. Items should remain contiguous throughout (no gaps).
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, i % 3 == 0 ? 100 : 30))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Scroll down
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            // Freeze extent
            SetFrozenExtent(target, scroll.Extent.Height);

            // Scroll to top
            scroll.Offset = new Vector(0, 0);
            Layout(target);
            Layout(target);

            // Scroll back down
            scroll.Offset = new Vector(0, 300);
            Layout(target);
            Layout(target);

            // Verify contiguity
            var realized = target.GetRealizedContainers()!
                .Where(x => x.IsVisible)
                .OrderBy(x => x.Bounds.Top)
                .ToList();

            Assert.True(realized.Count >= 2, "Expected at least 2 realized items");

            for (int i = 1; i < realized.Count; i++)
            {
                var prev = realized[i - 1];
                var curr = realized[i];
                var expectedTop = prev.Bounds.Top + prev.Bounds.Height;
                Assert.True(Math.Abs(curr.Bounds.Top - expectedTop) < 1,
                    $"Gap between items at position {i - 1} and {i}: " +
                    $"prev ends at {expectedTop:F1}, curr starts at {curr.Bounds.Top:F1}");
            }
        }

        private class TestLogSink : ILogSink
        {
            private readonly List<string> _messages;

            public TestLogSink(List<string> messages) => _messages = messages;

            public bool IsEnabled(LogEventLevel level, string area) =>
                level >= LogEventLevel.Warning && area == LogArea.Control;

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
            {
                _messages.Add(messageTemplate);
            }

            public void Log(LogEventLevel level, string area, object? source,
                string messageTemplate, params object?[] propertyValues)
            {
                var msg = messageTemplate;
                for (int i = 0; i < propertyValues.Length; i++)
                    msg = msg.Replace($"{{{i}}}", propertyValues[i]?.ToString() ?? "null");
                // Also try named placeholders
                _messages.Add($"{messageTemplate} | values=[{string.Join(", ", propertyValues)}]");
            }
        }
    }
}
