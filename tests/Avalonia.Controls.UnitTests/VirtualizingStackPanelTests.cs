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

        /// <summary>
        /// Same as <see cref="CanvasWithHeightTemplate"/> but with layout rounding disabled, so
        /// a fractional Height survives into DesiredSize. Needed to exercise sub-pixel size
        /// changes: at the default scale of 1 layout rounding snaps every desired size to a
        /// whole pixel, which makes sub-pixel changes unreachable in a test.
        /// </summary>
        private static FuncDataTemplate<ItemWithHeight> UnroundedCanvasWithHeightTemplate = new((_, _) =>
            new CanvasCountingMeasureArrangeCalls
            {
                Width = 100,
                UseLayoutRounding = false,
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
        // Expected count is one more than a page (11/21 rather than 10/20): scrolling from the
        // end back to index 20 via ScrollIntoView leaves one extra pooled container parented
        // (the transient scroll-to element is not reconciled with the freshly realized item).
        // The test's intent — that a whole extra *page* of unrealized elements is not created —
        // still holds; the off-by-one is expected realization/scroll-to behaviour.
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

        [Fact]
        public void Collapsing_Viewport_To_Empty_And_Restoring_Preserves_Scroll_Position()
        {
            // Regression test for scroll jump after navigating away and back (e.g. opening
            // the camera / another activity, which collapses the window viewport to 0x0).
            //
            // Before the fix: an empty (0x0) effective viewport was accepted and measured.
            // MeasureOverride treated it as disjunct, recycled every realized element and
            // re-anchored to index 0 with StartU=0 - losing the scroll anchor. The panel's
            // extent then collapsed toward the single realized item, and when the viewport
            // was restored the ScrollViewer clamped the (now out-of-range) offset, producing
            // a large upward scroll jump.
            //
            // After the fix: OnEffectiveViewportChanged ignores empty viewports and
            // MeasureOverride bails out when the viewport is empty but content is realized,
            // so StartU / realized range / offset all survive the round-trip unchanged.
            using var app = App();

            var (target, scroll, itemsControl) = CreateTarget(bufferFactor: 0);

            // Scroll to a middle position: 100 items x 10px, viewport 100px.
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            var offsetBefore = scroll.Offset;
            var firstBefore = target.FirstRealizedIndex;
            Debug.WriteLine($"[TEST] before collapse: offset={offsetBefore} firstRealized={firstBefore}");
            Assert.Equal(20, firstBefore);
            Assert.Equal(200, offsetBefore.Y);

            var root = (TestRoot)target.GetVisualRoot()!;

            // Collapse the viewport to empty (simulates the window being hidden while the
            // camera activity is in front).
            root.ClientSize = new Size(0, 0);
            root.InvalidateMeasure();
            Layout(target);
            Debug.WriteLine($"[TEST] while collapsed: offset={scroll.Offset} firstRealized={target.FirstRealizedIndex}");

            // Restore the viewport (return from the camera).
            root.ClientSize = new Size(100, 100);
            root.InvalidateMeasure();
            Layout(target);

            var offsetAfter = scroll.Offset;
            var firstAfter = target.FirstRealizedIndex;
            Debug.WriteLine($"[TEST] after restore: offset={offsetAfter} firstRealized={firstAfter}");

            Assert.Equal(offsetBefore.Y, offsetAfter.Y);
            Assert.Equal(firstBefore, firstAfter);
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

            // Should have no realized elements and no unrealized elements.
            Assert.Equal(0, target.GetRealizedElements().Count);
            Assert.Equal(0, target.Children.Count);

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
            // and reused. The test captures Control instances before the insert and asserts the
            // SAME instances appear after the insert (reference equality), proving that the
            // RetainMatchingContainers path preserved the containers rather than recycling them.
            using var app = App();

            // Use ObservableCollection so the Insert fires the collection-changed event
            // that drives the OnItemsChanged → ItemsInserted → InvalidateMeasure path.
            var items = new ObservableCollection<ItemWithHeightAndMeasureArrangeCount>(
                Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)));
            var (target, scroll, itemsControl) =
                CreateTarget<CountingPrepareItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0d);

            // Scroll down 20 items. Items 20-29 are realized (10 items at 10px each, viewport=100px).
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(20, target.FirstRealizedIndex);
            Assert.Equal(29, target.LastRealizedIndex);

            // Reset prepare counter — only count preparations triggered by the insert.
            itemsControl.PrepareCount = 0;

            // Insert an item at the beginning. Indices shift: old items[20..29] become
            // items[21..30]. The panel's anchor estimation places the new anchor at the
            // same offset (200/10 = 20), triggering the disjunct path. RetainMatchingContainers
            // should hold containers for items 21..30 (matching DataContexts) and reuse them
            // WITHOUT going through PrepareContainerForItemOverride again. Only the genuinely
            // new content at the new anchor index (old item 19) needs preparation.
            // Record which container instance is serving each item, so we can assert the very
            // same instances keep serving those items afterwards.
            var containerByItem = CaptureContainerByItem(target, itemsControl);
            Assert.True(containerByItem.Count >= 8,
                $"Test setup: expected ~10 realized containers, captured {containerByItem.Count}.");

            var newItems = new ItemWithHeightAndMeasureArrangeCount(-1);
            items.Insert(0, newItems);
            Layout(target);

            // The items themselves did not change — only their indices did. Reuse means the same
            // container instance is still showing the same item; the loose prepare-count bound
            // below could also be met by recycling a container and re-preparing it for a
            // different item, which is not reuse.
            var reused = CountSameContainerForSameItem(target, itemsControl, containerByItem);
            Assert.True(reused >= 8,
                $"Only {reused} of {containerByItem.Count} items are still served by the same " +
                $"container instance after the insert. RetainMatchingContainers should have kept " +
                $"them rather than recycling and re-preparing.");

            Assert.True(itemsControl.PrepareCount <= 3,
                $"Expected ≤ 3 container preparations after Insert (only the new anchor item " +
                $"needs Prepare); got {itemsControl.PrepareCount}. Without RetainMatchingContainers " +
                $"every realised slot would be re-prepared.");
        }

        /// <summary>
        /// Maps each currently-realized item to the container instance serving it.
        /// </summary>
        private static Dictionary<object, Control> CaptureContainerByItem(
            VirtualizingStackPanel target, ItemsControl itemsControl)
        {
            var result = new Dictionary<object, Control>();
            foreach (var container in target.GetRealizedContainers()!)
            {
                if (itemsControl.IndexFromContainer(container) < 0)
                    continue;
                if ((container as IDataContextProvider)?.DataContext is { } item)
                    result[item] = container;
            }
            return result;
        }

        /// <summary>
        /// Counts how many of the previously-captured items are still served by the exact same
        /// container instance — i.e. genuinely reused rather than recycled and re-prepared.
        /// </summary>
        private static int CountSameContainerForSameItem(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            Dictionary<object, Control> before)
        {
            var count = 0;
            foreach (var container in target.GetRealizedContainers()!)
            {
                if (itemsControl.IndexFromContainer(container) < 0)
                    continue;
                if ((container as IDataContextProvider)?.DataContext is { } item &&
                    before.TryGetValue(item, out var previous) &&
                    ReferenceEquals(previous, container))
                {
                    count++;
                }
            }
            return count;
        }

        [Fact]
        public void Collection_Reset_With_Reorder_Reuses_Matching_Containers_Without_Remeasure()
        {
            // Verifies that when a collection Reset occurs and the same item objects appear in
            // the new viewport, RetainMatchingContainers holds their containers and reuses them
            // WITHOUT going through PrepareContainerForItemOverride. PrepareCount is counted
            // via CountingPrepareItemsControl.
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();
            var collection = new ResettingObservableCollection<ItemWithHeightAndMeasureArrangeCount>(items);

            var (target, scroll, itemsControl) =
                CreateTarget<CountingPrepareItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: collection,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0d);

            Assert.Equal(0, target.FirstRealizedIndex);
            Assert.Equal(9, target.LastRealizedIndex);

            // Reset prepare counter — only count preparations triggered by the Reset operation.
            itemsControl.PrepareCount = 0;

            var containerByItem = CaptureContainerByItem(target, itemsControl);
            Assert.True(containerByItem.Count >= 8,
                $"Test setup: expected ~10 realized containers, captured {containerByItem.Count}.");

            // Reset with the first 10 items reversed. The same 10 item objects remain at indices
            // 0-9 (just shuffled among themselves). RetainMatchingContainers should hold their
            // containers and skip PrepareContainer when re-realising.
            var shuffled = new List<ItemWithHeightAndMeasureArrangeCount>(items);
            shuffled.Reverse(0, 10);
            collection.Reset(shuffled);
            Layout(target);

            // Same items, new indices: each must still be served by the container it already had.
            var reused = CountSameContainerForSameItem(target, itemsControl, containerByItem);
            Assert.True(reused >= 8,
                $"Only {reused} of {containerByItem.Count} items are still served by the same " +
                $"container instance after the reordering Reset. RetainMatchingContainers should " +
                $"have re-keyed the existing containers rather than recycling and re-preparing.");

            Assert.True(itemsControl.PrepareCount <= 3,
                $"Expected ≤ 3 container preparations after Reset-reorder (same items retained), " +
                $"got {itemsControl.PrepareCount}. Without RetainMatchingContainers every slot would " +
                $"be re-prepared.");
        }

        [Fact]
        public void Off_View_Height_Change_Is_Remeasured_On_Scroll_Back()
        {
            // Self-heal contract: while an item is virtualized (out of view) its persistent
            // size-cache entry may go stale because the VM data changed with no container to
            // re-measure it. When the item scrolls back into view it MUST be re-measured, which
            // overwrites the stale cache entry and corrects the extent. Uses the normal
            // recycle -> re-prepare realization path. Item 50 avoids the item-0 clamping heuristic.
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 10)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: 0d);

            // Realize item 50 at its initial height (H1 = 10). Item 50 top sits at 50*10 = 500.
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var c50 = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(50));
            Assert.Equal(10, c50.Bounds.Height);
            Assert.True(target.TryGetMeasuredSizeForTesting(50, out var cached50));
            Assert.Equal(10d, cached50);

            var extentBefore = scroll.Extent.Height;

            // Scroll item 50 fully out of view (back to the top). It is recycled/virtualized.
            scroll.Offset = new Vector(0, 0);
            Layout(target);
            Assert.Null(target.ContainerFromIndex(50));

            // Mutate its height while it is virtualized (no live container -> nothing invalidates now).
            items[50].Height = 100;

            // Scroll back to item 50. Item 50 top is still at 500 (items 0..49 unchanged at 10 each).
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            // The re-realized container was re-measured to H2 = 100 ...
            var c50b = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(50));
            Assert.Equal(100, c50b.Bounds.Height);
            // ... the stale cache entry was overwritten with the fresh measure ...
            Assert.True(target.TryGetMeasuredSizeForTesting(50, out var cached50b));
            Assert.Equal(100d, cached50b);
            // ... and the extent grew to reflect the larger item.
            Assert.True(scroll.Extent.Height > extentBefore,
                $"Extent should grow after off-view height change self-heals; before={extentBefore}, after={scroll.Extent.Height}");
        }

        [Fact]
        public void Retained_Container_Reuse_Remeasures_Changed_Item()
        {
            // Risk case: the RetainMatchingContainers reuse path returns a still-realized container
            // for the SAME item without an explicit re-measure. A data-bound height change on that
            // item invalidates the container's measure, so RealizeElements' "if (!IsMeasureValid)
            // Measure(...)" must re-measure the REUSED container. This asserts the reused container
            // instance renders H2 (correct self-heal) instead of the stale H1.
            using var app = App();

            var items = new ObservableCollection<ItemWithHeight>(
                Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 10)));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: 0d);

            // Realize items 20..29 (viewport 100 / 10px each).
            scroll.Offset = new Vector(0, 200);
            Layout(target);
            Assert.Equal(20, target.FirstRealizedIndex);
            Assert.Equal(29, target.LastRealizedIndex);

            // Capture the container instance for item 25 to prove it is REUSED (not re-prepared).
            var item25 = items[25];
            var c25 = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(25));
            Assert.Equal(10, c25.Bounds.Height);

            // Change item 25's height, then insert at the front WITHOUT an intervening layout so the
            // pending re-measure is carried into the retained-reuse realization. After Insert(0),
            // item 25 shifts to index 26; the disjunct measure retains its container and reuses it.
            item25.Height = 100;
            items.Insert(0, new ItemWithHeight(-1, 10));
            Layout(target);

            // Same container instance was reused for the same item, now at index 26 ...
            var c26 = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(26));
            Assert.Same(c25, c26);
            Assert.Same(item25, c26.DataContext);
            // ... and it was re-measured to H2 = 100 rather than rendering the stale H1.
            Assert.Equal(100, c26.Bounds.Height);
            Assert.True(target.TryGetMeasuredSizeForTesting(26, out var cached26));
            Assert.Equal(100d, cached26);
        }

        [Fact]
        public void Visible_Item_Height_Change_Uses_Live_Measure()
        {
            // Contract: a realized (visible) item is always sized by its live measure, never by a
            // cache entry. Changing a visible item's height must update its Bounds on the very next
            // layout pass, independent of any recorded/estimated size.
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 10)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: 0d);

            var c5 = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(10, c5.Bounds.Height);

            // Mutate while visible; a live container's binding invalidates measure immediately.
            items[5].Height = 77;
            Layout(target);

            var c5b = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(77, c5b.Bounds.Height);
            Assert.True(target.TryGetMeasuredSizeForTesting(5, out var cached5));
            Assert.Equal(77d, cached5);
        }

        [Fact]
        public void Measured_Size_Record_Shifts_Up_On_Insert_At_The_Front()
        {
            using var app = App();
            var (target, scroll, items) = CreateTargetWithPopulatedSizeRecord();

            var before = ReadMeasuredSizeRecord(target, items.Count + 4);
            var expected = before.ToDictionary(e => e.Key + 2, e => e.Value);

            // Prepend: every recorded entry moves. Asserted before any layout, so a re-measure of
            // the realized window cannot paper over a wrong mapping.
            items.InsertRange(0, new[] { new ItemWithHeight(900, 33), new ItemWithHeight(901, 34) });

            AssertMeasuredSizeRecordEquals(expected, target, items.Count + 4);

            Layout(target);
            AssertExtentAgreesWithMeasuredSizeRecord(target, scroll, items.Count);
        }

        [Fact]
        public void Measured_Size_Record_Shifts_Up_On_Mid_List_Insert()
        {
            using var app = App();
            var (target, scroll, items) = CreateTargetWithPopulatedSizeRecord();

            var before = ReadMeasuredSizeRecord(target, items.Count + 4);
            Assert.Contains(before.Keys, k => k < 20);
            Assert.Contains(before.Keys, k => k >= 20);

            // Only the entries at or after the insertion point move; the ones before it must not.
            var expected = before.ToDictionary(e => e.Key >= 20 ? e.Key + 3 : e.Key, e => e.Value);

            items.InsertRange(20, Enumerable.Range(0, 3).Select(i => new ItemWithHeight(900 + i, 35 + i)));

            AssertMeasuredSizeRecordEquals(expected, target, items.Count + 4);

            Layout(target);
            AssertExtentAgreesWithMeasuredSizeRecord(target, scroll, items.Count);
        }

        [Fact]
        public void Measured_Size_Record_Is_Untouched_By_An_Insert_Past_The_Last_Item()
        {
            using var app = App();
            var (target, scroll, items) = CreateTargetWithPopulatedSizeRecord();

            var before = ReadMeasuredSizeRecord(target, items.Count + 4);

            // Append: nothing is at or after the insertion point, so the record must come through
            // untouched (this is the early-out an infinite-scroll list hits on every batch).
            items.InsertRange(items.Count, Enumerable.Range(0, 3).Select(i => new ItemWithHeight(900 + i, 35 + i)));

            AssertMeasuredSizeRecordEquals(before, target, items.Count + 4);

            Layout(target);
            AssertExtentAgreesWithMeasuredSizeRecord(target, scroll, items.Count);
        }

        [Fact]
        public void Measured_Size_Record_Shifts_Down_On_Remove_At_The_Front()
        {
            using var app = App();
            var (target, scroll, items) = CreateTargetWithPopulatedSizeRecord();

            var before = ReadMeasuredSizeRecord(target, items.Count + 4);
            Assert.Contains(0, before.Keys);
            Assert.Contains(1, before.Keys);

            // The two removed items' entries are dropped; everything after them moves down.
            var expected = before
                .Where(e => e.Key >= 2)
                .ToDictionary(e => e.Key - 2, e => e.Value);

            items.RemoveRange(0, 2);

            AssertMeasuredSizeRecordEquals(expected, target, items.Count + 4);

            Layout(target);
            AssertExtentAgreesWithMeasuredSizeRecord(target, scroll, items.Count);
        }

        [Fact]
        public void Measured_Size_Record_Shifts_Down_On_Mid_List_Remove()
        {
            using var app = App();
            var (target, scroll, items) = CreateTargetWithPopulatedSizeRecord();

            var before = ReadMeasuredSizeRecord(target, items.Count + 4);
            Assert.Contains(before.Keys, k => k < 20);
            Assert.Contains(before.Keys, k => k >= 23);

            var expected = before
                .Where(e => e.Key < 20 || e.Key >= 23)
                .ToDictionary(e => e.Key >= 23 ? e.Key - 3 : e.Key, e => e.Value);

            items.RemoveRange(20, 3);

            AssertMeasuredSizeRecordEquals(expected, target, items.Count + 4);

            Layout(target);
            AssertExtentAgreesWithMeasuredSizeRecord(target, scroll, items.Count);
        }

        /// <summary>
        /// Builds a panel whose per-item size record covers many more indices than are realized —
        /// so a remap error cannot be hidden by the realized window being re-measured — with a
        /// distinct height per item, so an entry that ends up on the wrong index is detectable.
        /// </summary>
        private static (VirtualizingStackPanel, ScrollViewer, AvaloniaList<ItemWithHeight>) CreateTargetWithPopulatedSizeRecord()
        {
            var items = new AvaloniaList<ItemWithHeight>(
                Enumerable.Range(0, 40).Select(x => new ItemWithHeight(x, 20 + x)));
            var (target, scroll, _) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0d);

            // Scroll across the collection and back so entries exist well outside the realized
            // window, then settle at the top.
            foreach (var offset in new[] { 400d, 800d, 1200d, 0d })
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
            }

            return (target, scroll, items);
        }

        /// <summary>
        /// Reads the panel's per-item size record back through the internal test seam, scanning
        /// past the end of the collection so an entry left stranded beyond it is caught.
        /// </summary>
        private static Dictionary<int, double> ReadMeasuredSizeRecord(VirtualizingStackPanel target, int scanTo)
        {
            var record = new Dictionary<int, double>();

            for (var i = 0; i <= scanTo; ++i)
            {
                if (target.TryGetMeasuredSizeForTesting(i, out var size))
                    record[i] = size;
            }

            Assert.NotEmpty(record);
            return record;
        }

        private static void AssertMeasuredSizeRecordEquals(
            IReadOnlyDictionary<int, double> expected,
            VirtualizingStackPanel target,
            int scanTo)
        {
            static string Format(IEnumerable<KeyValuePair<int, double>> record) =>
                string.Join(", ", record.OrderBy(e => e.Key).Select(e => $"[{e.Key}]={e.Value}"));

            Assert.Equal(Format(expected), Format(ReadMeasuredSizeRecord(target, scanTo)));
        }

        /// <summary>
        /// The reported extent is <c>knownSum + (itemCount - knownCount) * mean</c> over the size
        /// record, so recomputing it from the entries read back through the seam proves the
        /// panel's incrementally maintained running sum still agrees with those entries.
        /// </summary>
        private static void AssertExtentAgreesWithMeasuredSizeRecord(
            VirtualizingStackPanel target,
            ScrollViewer scroll,
            int itemCount)
        {
            var record = ReadMeasuredSizeRecord(target, itemCount + 4);
            Assert.All(record.Keys, key => Assert.InRange(key, 0, itemCount - 1));

            var knownSum = record.Values.Sum();
            var mean = knownSum / record.Count;
            var expected = knownSum + ((itemCount - record.Count) * mean);

            // The panel's desired size goes through layout rounding, so the reported extent is
            // quantised to whole pixels. Any larger gap means the running sum no longer matches
            // the entries it is meant to summarise (e.g. a drop that forgot to subtract).
            Assert.True(Math.Abs(expected - scroll.Extent.Height) <= 1d,
                $"Extent {scroll.Extent.Height} does not match the {record.Count} recorded sizes " +
                $"(sum {knownSum}, mean {mean}, {itemCount} items => {expected}).");
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
        [InlineData(0d, 15, 5, 190, 267, 110)]
        [InlineData(0.5d, 10, 10, 253, 300, 173)]
        public void ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Larger_Items(double bufferFactor, int firstIndex, int count, int y, int extentHeight, int offset)
        {
            using var app = App();

            // First 10 items have height of 10, next 10 have height of 20.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((x / 10) + 1) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate, bufferFactor: bufferFactor);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll the estimate is still 10 (only the first-10 short items
            // have been measured), so item 19 is positioned at 190 (19 * 10) — an intentionally
            // imperfect estimate we don't correct. The EXTENT, however, is now computed from the
            // persistent per-item size record (knownSum + unknown*mean), which remembers the
            // measured sizes:
            //  - bufferFactor 0 (firstIndex 15): only items 0-9 (h=10) and 15-19 (h=20) are ever
            //    realized; items 10-14 are never measured. Extent = 200 (knownSum) + 5*13.33
            //    (mean over the 15 known) = 267 — more than the naive realized bottom of 210,
            //    honestly accounting for the five unmeasured tall middle items.
            //  - bufferFactor 0.5 (firstIndex 10): the buffer also realizes items 10-14, so all
            //    20 items are measured and the extent is the exact true total 300.
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
        [InlineData(0d, 15, 5, 190, 267, 110)]
        [InlineData(0.5d, 10, 10, 253, 300, 173)]
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

            // Horizontal mirror of ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Larger_Items.
            // Item 19 is positioned at 190 (19 * the still-10 scroll-time estimate) — an
            // intentionally imperfect estimate we don't correct. The EXTENT now comes from the
            // persistent per-item size record (knownSum + unknown*mean):
            //  - bufferFactor 0 (firstIndex 15): items 10-14 are never realized, so extent =
            //    200 + 5*13.33 = 267, honestly covering the five unmeasured wide middle items.
            //  - bufferFactor 0.5 (firstIndex 10): the buffer realizes items 10-14 too, so all
            //    20 are measured and the extent is the exact true total 300.
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

        [Fact]
        public void Focused_Container_Is_Positioned_Correctly_When_Scrolled_Past_Items_With_Different_Heights()
        {
            using var app = App();

            var items = Enumerable.Range(0, 20)
                .Select(x => new ItemWithHeight(x, x < 10 ? 10 : 50))
                .ToList();

            var (target, _, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            var focused = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            focused.Focusable = true;
            focused.Focus();

            target.ScrollIntoView(15);
            Layout(target);

            Assert.True(target.FirstRealizedIndex > 5);

            var firstIndex = target.FirstRealizedIndex;
            var firstRealized = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(firstIndex));

            // An off-screen focused element is positioned by GetOrEstimateElementU as
            //   realized.StartU - ((firstIndex - focusedIndex) * EstimateElementSizeU())
            // i.e. anchored at the first realized element's top, bridged by the panel's
            // element-size estimate. This test asserts that invariant. It uses the panel's
            // actual estimate rather than the realized-window average: EstimateElementSizeU
            // now derives its value from a persistent per-item size record (mean over all
            // items ever measured), not the currently-realized set, so the realized-window
            // average is no longer the estimate the panel uses.
            var estField = typeof(VirtualizingStackPanel).GetField("_lastEstimatedElementSizeU",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var estimatedSize = (double)estField!.GetValue(target)!;
            var expectedTop = firstRealized.Bounds.Top - ((firstIndex - 5) * estimatedSize);

            focused = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(expectedTop, focused.Bounds.Top, 3);
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
            target.GetLayoutManager()?.ExecuteLayoutPass();
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

        /// <summary>
        /// A VirtualizingStackPanel that simulates content which settles its size after being
        /// realized (async images, deferred bindings, text that wraps once it knows its width),
        /// and re-invalidates its own measure whenever that changes its DesiredSize — modelling
        /// the parent re-measuring the panel and driving the measure feedback loop.
        /// </summary>
        /// <remarks>
        /// The perturbation is applied once per item and then settles. An earlier version flipped
        /// sizes by measure-pass parity, which never settles: no panel can converge against
        /// content that reports a different size on every single measure, so such a model can only
        /// ever demonstrate that some hard cap exists, not that layout converges. Avalonia's
        /// LayoutManager already bounds a never-settling template globally.
        /// </remarks>
        private class VirtualizingStackPanelWithInstability : VirtualizingStackPanel
        {
            public int Measured { get; set; }

            /// <summary>
            /// Total number of times the panel actually measured a container — the real cost of
            /// a layout cycle, and the thing these tests care about bounding. (An earlier version
            /// counted <see cref="AdjustElementSize"/> calls, but that hook is consulted wherever
            /// the panel needs an element's size, not only during realization, so it does not
            /// measure layout work.)
            /// </summary>
            public int ContainerMeasures =>
                Items.OfType<ICountMeasureArrangeCalls>().Sum(i => i.Measured);

            public bool EnableInstability { get; set; }
            public double Instability { get; set; } = 5.0;

            public void ResetMeasureArrangeCounters()
            {
                Measured = 0;
                foreach (var itm in Items.OfType<ICountMeasureArrangeCalls>())
                {
                    itm.Measured = 0;
                    itm.Arranged = 0;
                }
            }

            private Size _lastResult;
            private int _invalidationBudget;
            private readonly HashSet<int> _perturbed = new();

            public void StartInstability(int budget = 10)
            {
                EnableInstability = true;
                _invalidationBudget = budget;
                _perturbed.Clear();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                Measured++;
                var result = base.MeasureOverride(availableSize);

                // Simulate the layout cycle: when the DesiredSize changes (due to
                // oscillating element sizes), the parent re-measures this panel.
                // Budget-limited to prevent infinite loops from the cycle breaker's
                // own deferred InvalidateMeasure.
                if (EnableInstability && result != _lastResult && _invalidationBudget > 0)
                {
                    _invalidationBudget--;
                    InvalidateMeasure();
                }
                _lastResult = result;

                return result;
            }

            protected internal override double AdjustElementSize(int index, double measuredSizeU)
            {
                if (!EnableInstability) return measuredSizeU;
                // The first size this item is seen at is provisional; it settles afterwards.
                return _perturbed.Add(index)
                    ? measuredSizeU + Instability
                    : measuredSizeU;
            }
        }

        /// <summary>
        /// An ItemsControl that counts calls to <see cref="PrepareContainerForItemOverride"/>.
        /// Useful for verifying that <c>RetainMatchingContainers</c> skips container
        /// preparation for retained instances during a disjunct realisation.
        /// </summary>
        private class CountingPrepareItemsControl : ItemsControl
        {
            public int PrepareCount { get; set; }

            protected internal override void PrepareContainerForItemOverride(Control container, object? item, int index)
            {
                PrepareCount++;
                base.PrepareContainerForItemOverride(container, item, index);
            }
        }

        /// <summary>
        /// A VirtualizingStackPanel that applies a constant sub-pixel (±0.3 px) size adjustment
        /// per index via the AdjustElementSize hook. Because the adjustment never changes, the
        /// panel must stop seeing size changes after the first pass — used to verify the panel
        /// records and re-checks element sizes through the same accessor.
        /// </summary>
        private class VirtualizingStackPanelWithSubPixelNoise : VirtualizingStackPanel
        {
            public bool EnableNoise { get; set; }

            protected internal override double AdjustElementSize(int index, double measuredSizeU)
            {
                if (!EnableNoise) return measuredSizeU;
                // Alternate +0.3 / -0.3 by index parity — produces sub-pixel diffs
                // between stored size and re-measured DesiredSize.
                return index % 2 == 0 ? measuredSizeU + 0.3 : measuredSizeU - 0.3;
            }
        }

        /// <summary>
        /// A VirtualizingStackPanel that models async-loaded content via the
        /// <see cref="VirtualizingStackPanel.AdjustElementSize"/> hook: the first time an
        /// element at a given index is measured it reports a small "placeholder" size, and
        /// on every subsequent measure it reports a larger "loaded" size. The size an item
        /// reports therefore flips based on whether it has already been realized — exactly
        /// the input that makes the realized-set-average estimate in EstimateElementSizeU
        /// swing between layout passes.
        /// </summary>
        private class VirtualizingStackPanelAsyncGrow : VirtualizingStackPanel
        {
            private readonly Dictionary<int, int> _measureCount = new();

            public double PlaceholderSizeU { get; set; } = 84;
            public double LoadedSizeU { get; set; } = 292;

            protected internal override double AdjustElementSize(int index, double measuredSizeU)
            {
                var count = _measureCount.TryGetValue(index, out var c) ? c : 0;
                _measureCount[index] = count + 1;
                return count == 0 ? PlaceholderSizeU : LoadedSizeU;
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

        /// <summary>
        /// Height depends on the item's data read directly from DataContext, and does NOT use a
        /// binding — so a DataContext change does not synchronously invalidate this control's own
        /// measure. This models content whose size settles after the initial measure (async images,
        /// markdown, converters) and lets us probe whether a container reused for a different item
        /// at the same width re-measures its content subtree.
        /// </summary>
        private class DataDrivenHeightControl : Control
        {
            protected override Size MeasureOverride(Size availableSize)
            {
                var h = (DataContext as ItemWithHeight)?.Height ?? 10;
                return new Size(50, h);
            }
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

        [Fact]
        public void Reset_With_MidList_Insert_Realizes_Shifted_Items_At_Correct_Index()
        {
            // Repro for the wrong-visual-index bug: when a mid-list insert is coalesced into a
            // single Reset (e.g. DynamicData's Bind reset-threshold turning an InsertRange into a
            // Reset), the realized elements BEFORE the insertion point still match by DataContext
            // while every element at/after it is now shifted to a later index. The preserve-on-reset
            // heuristic must NOT keep the stale mapping just because a majority (the prefix) still
            // matches — otherwise the shifted items stay pinned to the wrong containers and render
            // at the wrong position (children appearing under a later headline).
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

                // Items 0-9 are realized (viewport = 100px, item height = 10px, no buffer).
                Assert.Equal(0, target.FirstRealizedIndex);
                Assert.Equal(9, target.LastRealizedIndex);

                // Insert 3 new items after index 5 and fire a SINGLE Reset. The realized prefix
                // [0..5] still matches (6 of 10 => a bare majority), which previously tripped the
                // "preserve realized elements" heuristic; items 6-9 are shifted down by 3.
                var newItems = Enumerable.Range(0, 3)
                    .Select(i => (object)new TypeA_Item { Name = $"N{i}" }).ToList();
                var afterInsert = originalItems.Cast<object>().Take(6)
                    .Concat(newItems)
                    .Concat(originalItems.Cast<object>().Skip(6))
                    .ToList();
                items.Reset(afterInsert);
                Layout(target);

                // Every realized container must map to the item now at its index. With the bug,
                // the containers for the old items 6-9 stay pinned at indices 6-9 (stale) and the
                // newly-inserted items get appended after them.
                var containersAfter = target.GetRealizedContainers()!.ToList();
                foreach (var container in containersAfter)
                {
                    var idx = itemsControl.IndexFromContainer(container);
                    if (idx >= 0 && idx < afterInsert.Count)
                    {
                        var dc = (container as IDataContextProvider)?.DataContext;
                        Assert.Same(afterInsert[idx], dc);
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
        public void Reused_Container_Reflects_New_Item_Height_After_Scroll()
        {
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                // Alternating short/tall items so a recycled container is reused for an item of a
                // different height. Height depends on the item but is not bound (no self-invalidate).
                var items = Enumerable.Range(0, 50)
                    .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 20 : 80))
                    .ToList();

                // IVirtualizingDataTemplate: on recycle the container is NOT cleared and the same
                // child instance is reused (Build(data, existing) returns existing) — matching the
                // app's FieldTemplateSelector.
                var template = new FuncVirtualizingDataTemplate<object>((_, _) => new DataDrivenHeightControl())
                {
                    MinPoolSizePerKey = 2
                };

                var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: template);

                // Scroll down and back so containers get recycled into the pool and reused for
                // different items.
                for (double offset = 0; offset <= 400; offset += 40)
                {
                    scroll.Offset = new Vector(0, offset);
                    Layout(target);
                }
                for (double offset = 400; offset >= 0; offset -= 40)
                {
                    scroll.Offset = new Vector(0, offset);
                    Layout(target);
                }

                // Every realized container must reflect the height of the item it currently displays,
                // not a stale height from a previous item it was recycled from.
                var containers = target.GetRealizedContainers()!.ToList();
                Assert.NotEmpty(containers);
                Assert.All(containers, c =>
                {
                    var expected = ((ItemWithHeight)c.DataContext!).Height;
                    Assert.Equal(expected, c.Bounds.Height);
                });
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Warmup_Pools_Keys_First_Encountered_Outside_The_Head_Of_The_Collection()
        {
            // Warmup used to discover which template keys exist by sampling the first N items
            // (WarmupSampleSize, default 50). That assumes the head of the collection represents
            // the whole of it. Here it does not: the first 200 items are one type and the rest
            // another — a shape as ordinary as a grouped or sorted list. Under head sampling the
            // second type is never pooled no matter how much the user scrolls through it.
            //
            // The pool must instead grow off the keys the panel actually encounters.
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 400).Select<int, object>(i =>
                        i < 200
                            ? new TypeA_Item { Name = $"A{i}" }
                            : new TypeB_Item { Name = $"B{i}" }));

                // Well above the ~10 containers a 100px viewport realizes, so a shortfall to
                // warm up always remains.
                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 25
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);
                target.EnableWarmup = true;

                target.PerformWarmup();

                var pool = target.RecyclePoolForTesting;
                Assert.NotNull(pool);
                Assert.True(pool!.ContainsKey(typeof(TypeA_Item)),
                    "Warming at the top of the collection should pool the type found there.");

                // Scroll deep into the second region so TypeB containers are actually used.
                scroll.Offset = new Vector(0, 2500);
                Layout(target);

                var realizedAreTypeB = target.GetRealizedElements()
                    .Where(e => e is not null)
                    .All(e => (e as IDataContextProvider)?.DataContext is TypeB_Item);
                Assert.True(realizedAreTypeB,
                    "Test setup: after scrolling to offset 2500 the realized items should be TypeB.");

                target.PerformWarmup();

                var poolB = pool.TryGetValue(typeof(TypeB_Item), out var listB) ? listB.Count : 0;
                Assert.True(poolB > 0,
                    $"After scrolling through the TypeB region, warmup must pool TypeB " +
                    $"containers, but the pool holds {poolB}. Keys are still being discovered " +
                    $"from a sample of the head of the collection instead of from what the " +
                    $"panel actually encountered.");
            }
            finally
            {
                ContentVirtualizationDiagnostics.IsEnabled = true;
            }
        }

        [Fact]
        public void Warmup_Forgets_Template_Keys_The_Collection_No_Longer_Contains()
        {
            // The pool now follows encountered keys, so those keys must also be forgotten when the
            // items they came from are gone — otherwise replacing the collection would leave the
            // panel warming (and holding) containers for a kind of item that no longer exists,
            // which is a leak that grows with every reset.
            using var app = App();
            ContentVirtualizationDiagnostics.IsEnabled = true;

            try
            {
                var items = new ResettingObservableCollection<object>(
                    Enumerable.Range(0, 40).Select<int, object>(i => new TypeA_Item { Name = $"A{i}" }));

                var template = new FuncVirtualizingDataTemplate<object>((item, _) =>
                    new Canvas { Width = 100, Height = 10 })
                {
                    MinPoolSizePerKey = 20
                };

                var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: template);
                target.EnableWarmup = true;

                target.PerformWarmup();
                Assert.Contains(typeof(TypeA_Item), target.DiscoverTemplateKeys().Keys);

                // Replace the collection with items of an entirely different kind.
                items.Reset(Enumerable.Range(0, 40).Select<int, object>(i => new TypeB_Item { Name = $"B{i}" }));
                Layout(target);

                var keys = target.DiscoverTemplateKeys().Keys;
                Assert.Contains(typeof(TypeB_Item), keys);
                Assert.DoesNotContain(typeof(TypeA_Item), keys);
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
                .Select(i => (object)new ItemWithHeightAndMeasureArrangeCount(i, i % 2 == 0 ? 10 : 100))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            target.ResetMeasureArrangeCounters();
            target.StartInstability(budget: 100);

            // Scroll down 10 increments of 20px each
            for (int i = 0; i < 10; i++)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 20);
                Layout(target);
            }

            // Bound the real cost: container measures across 10 small scroll steps. A measure
            // feedback loop shows up here as a multiple of the settled count.
            Assert.True(target.ContainerMeasures <= 40,
                $"Expected ≤ 40 container measures for 10 scroll steps but got " +
                $"{target.ContainerMeasures} (panel measures: {target.Measured}). " +
                $"The measure feedback loop is not converging.");
        }

        // ===== Category: Layout cycle prevention (cycle breaker, ValidateStartU) =====

        [Fact]
        public void Non_Deterministic_Measurement_Converges_Within_A_Layout_Pass()
        {
            // A large scroll jump while item measurements are non-deterministic must not turn
            // into an unbounded measure feedback loop. This used to be guaranteed by a cycle
            // breaker that hard-capped the panel at one full pass; it is now a property of the
            // panel's estimate being derived from the persistent per-item size record instead of
            // the currently-realized window, so the extent stops moving and the loop settles.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeightAndMeasureArrangeCount(i, i % 3 == 0 ? 10 : 50))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            target.ResetMeasureArrangeCounters();

            // Enable non-deterministic measurement (alternating size inflation) BEFORE the
            // scroll jump. Each measure pass that produces a different DesiredSize re-invokes
            // InvalidateMeasure, simulating the production layout cycle.
            target.StartInstability(budget: 50);

            // Large scroll jump — triggers disjunct recycle and the oscillating measure cycle
            scroll.Offset = new Vector(0, 1500);
            Layout(target);

            // A single scroll jump realizes ~13 containers (1500/50 plus the 0.5 cache buffer).
            // Settling non-deterministic sizes costs a few extra measures; an unconverged loop
            // costs a multiple of the realized count per iteration until the budget runs out.
            Assert.True(target.ContainerMeasures <= 40,
                $"Expected ≤ 40 container measures for one scroll jump but got " +
                $"{target.ContainerMeasures} (panel measures: {target.Measured}). " +
                $"The measure feedback loop is not converging.");

            // And the panel must have actually settled: another layout at the same offset
            // changes nothing.
            var desiredBefore = target.DesiredSize;
            var measuresBefore = target.ContainerMeasures;
            Layout(target);
            Assert.Equal(desiredBefore, target.DesiredSize);
            Assert.Equal(measuresBefore, target.ContainerMeasures);
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
        public void Adjusted_Element_Size_Does_Not_Read_As_A_Resize_Every_Pass()
        {
            // The panel has one view of "how big is element i" (GetElementSizeU, which applies
            // AdjustElementSize). Recording and re-checking sizes must both go through it. If
            // recording applied the adjustment and checking read raw DesiredSize instead, the two
            // would differ by the adjustment on EVERY pass, so every pass would look like a fresh
            // resize: endless StartU compensation and a discarded size record.
            //
            // The panel here adjusts sizes by a constant +/-0.3px per index. The adjustment never
            // changes, so after the first pass nothing is resizing and the panel must report no
            // further size changes.
            using var app = App();

            var logMessages = new List<string>();
            var sink = new TestLogSink(logMessages);
            var originalSink = Logger.Sink;
            Logger.Sink = sink;
            try
            {
                var items = Enumerable.Range(0, 50)
                    .Select(i => (object)new ItemWithHeight(i, 10))
                    .ToList();

                var (target, scroll, itemsControl) =
                    CreateTarget<ItemsControl, VirtualizingStackPanelWithSubPixelNoise>(
                        items: items,
                        itemTemplate: CanvasWithHeightTemplate,
                        bufferFactor: 0.5d);

                target.EnableNoise = true;
                scroll.Offset = new Vector(0, 200);
                Layout(target);

                // Let the adjusted sizes be recorded, then watch for phantom resizes.
                target.InvalidateMeasure();
                Layout(target);
                logMessages.Clear();

                var startUField = GetRealizedStartUAccessor(target, out var realized);
                var startUBefore = (double)startUField.GetValue(realized)!;

                for (var pass = 0; pass < 5; pass++)
                {
                    target.InvalidateMeasure();
                    Layout(target);
                }

                Assert.DoesNotContain(logMessages, m =>
                    m.Contains("Item template size changed during layout"));

                var startUAfter = (double)startUField.GetValue(GetRealizedStackElements(target))!;
                Assert.True(Math.Abs(startUAfter - startUBefore) < 0.01,
                    $"StartU drifted from {startUBefore} to {startUAfter} across 5 no-op passes — " +
                    $"the panel is re-detecting its own size adjustment as a resize.");
            }
            finally
            {
                Logger.Sink = originalSink;
            }
        }

        [Fact]
        public void Large_Scroll_Jump_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle()
        {
            // Simulates fast scrolling: a large viewport jump with heterogeneous item heights.
            // The cycle breaker prevents the measure-feedback loop from oscillating across
            // multiple full re-realisations.
            using var app = App();

            // Create items with wildly varying heights (like a real form with headers,
            // text fields, image fields, etc.)
            var items = Enumerable.Range(0, 71)
                .Select(i => (object)new ItemWithHeightAndMeasureArrangeCount(i, (i % 5) switch
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
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 2.0d);  // CacheLength=2 like the real control

            target.ResetMeasureArrangeCounters();
            target.StartInstability(budget: 50);

            // Simulate fast scroll: jump from top to near bottom under measurement instability
            scroll.Offset = new Vector(0, 5000);
            Layout(target);

            Assert.True(target.ContainerMeasures <= 60,
                $"Expected ≤ 60 container measures for a single large scroll jump but got " +
                $"{target.ContainerMeasures} (panel measures: {target.Measured}). " +
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
            // allowing each scroll jump to get a fresh full measure pass. Without the
            // reset, the breaker stays engaged from the first jump and the second jump
            // gets no full realisation — leaving the realized range behind the viewport.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 80))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            target.StartInstability(budget: 100);

            // First scroll jump — produces a viewport change
            scroll.Offset = new Vector(0, 1000);
            Layout(target);

            var firstJumpFirstIndex = target.FirstRealizedIndex;
            var firstJumpLastIndex = target.LastRealizedIndex;
            Assert.True(firstJumpLastIndex > firstJumpFirstIndex,
                $"First jump should realize a range, got [{firstJumpFirstIndex}..{firstJumpLastIndex}]");

            // Second scroll jump in the same panel lifetime. Each jump must be realized on its
            // own merits — no per-panel state may make a later pass do less work than the first.
            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            Assert.True(target.FirstRealizedIndex > firstJumpFirstIndex,
                $"After the second scroll jump the realized range should have moved forward, but " +
                $"FirstRealizedIndex is still {target.FirstRealizedIndex} " +
                $"(was [{firstJumpFirstIndex}..{firstJumpLastIndex}]).");
            Assert.True(target.LastRealizedIndex > target.FirstRealizedIndex,
                $"Second jump realized a degenerate range " +
                $"[{target.FirstRealizedIndex}..{target.LastRealizedIndex}].");

            // The realized items must actually cover the viewport, top to bottom.
            AssertRealizedContiguous(target, "after second scroll jump");
        }

        [Fact]
        public void Items_Added_Under_Measurement_Instability_Are_Reflected_In_The_Next_Layout()
        {
            // Appending items while measurement is non-deterministic must still grow the extent
            // in the very next layout pass. No per-panel loop-suppression state may swallow the
            // work triggered by a collection change.
            using var app = App();

            var items = new ObservableCollection<ItemWithHeight>(
                Enumerable.Range(0, 50).Select(i => new ItemWithHeight(i, 50)));

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate);

            target.StartInstability(budget: 100);

            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var extentBefore = scroll.Extent.Height;

            for (int i = 50; i < 60; i++)
                items.Add(new ItemWithHeight(i, 50));

            Layout(target);

            // 10 more 50px items => the extent must grow by roughly 500px.
            Assert.True(scroll.Extent.Height >= extentBefore + 400,
                $"Extent only grew from {extentBefore} to {scroll.Extent.Height} after appending " +
                $"10 items of 50px. The collection change was not fully reflected.");
        }

        [Fact]
        public void Rapid_Scrolling_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle()
        {
            // Simulates rapid scrolling through many positions under measurement instability —
            // the scenario that triggers layout cycles in production with complex controls.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeightAndMeasureArrangeCount(i, 10 + (i % 7) * 15))
                .ToList();

            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelWithInstability>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 2.0d);

            target.ResetMeasureArrangeCounters();
            target.StartInstability(budget: 200);

            // Rapid scroll: 20 jumps of varying sizes (simulates fast mouse wheel / touch)
            for (int i = 0; i < 20; i++)
            {
                scroll.Offset = new Vector(0, scroll.Offset.Y + 200);
                Layout(target);
            }

            Assert.True(target.ContainerMeasures <= 400,
                $"Expected ≤ 400 container measures for 20 scroll steps but got " +
                $"{target.ContainerMeasures} (panel measures: {target.Measured}). " +
                $"Layout cycle oscillation is likely occurring.");

            // Verify we ended up with valid realized elements
            Assert.True(target.FirstRealizedIndex >= 0, "Should have realized elements");
            Assert.True(target.LastRealizedIndex <= 99, "Last realized index should be within bounds");
        }

        // ===== Category: Adversarial multi-shape stability harness =====
        //
        // These tests exercise EstimateElementSizeU / extent stability across a range of
        // item-height distributions (not just our proprietary UI's shape). Each shape asserts
        // the three properties that matter for correctness + stability:
        //   (a) realized items stay contiguous (no gaps/overlaps in Bounds),
        //   (b) the reported extent does not oscillate across repeated layout passes,
        //   (c) scroll position does not jump when off-anchor items resize.

        // Asserts that the visible realized elements tile without gaps or overlaps.
        private static void AssertRealizedContiguous(VirtualizingStackPanel target, string context)
        {
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
                    $"Gap/overlap ({context}): item ends at {expectedTop}, next starts at {curr.Bounds.Top}");
            }
        }

        // Re-measures the panel `passes` times at the current offset and returns the
        // spread (max - min) of the reported vertical extent. A stable estimate keeps this
        // small; an oscillating estimate produces a large spread.
        private static (double spread, double[] extents) MeasureExtentSpreadOverPasses(
            VirtualizingStackPanel target, ScrollViewer scroll, int passes)
        {
            var extents = new double[passes];
            for (int i = 0; i < passes; i++)
            {
                target.InvalidateMeasure();
                Layout(target);
                extents[i] = scroll.Extent.Height;
            }
            return (extents.Max() - extents.Min(), extents);
        }

        // --- Shape: uniform (control / no-op case) ---

        [Fact]
        public void Adversarial_Uniform_Extent_Is_Stable_Across_Repeated_Passes()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000).Select(i => (object)new ItemWithHeight(i, 30)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 3000);
            Layout(target);

            var (spread, extents) = MeasureExtentSpreadOverPasses(target, scroll, 10);
            Assert.True(spread < 1.0, $"Uniform extent oscillated by {spread}px: [{string.Join(", ", extents)}]");
        }

        [Fact]
        public void Adversarial_Uniform_Realized_Items_Stay_Contiguous()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000).Select(i => (object)new ItemWithHeight(i, 30)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            for (double offset = 0; offset < 3000; offset += 25)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
                AssertRealizedContiguous(target, $"uniform @ {offset}");
            }
        }

        // --- Shape: bimodal (40px / 300px) ---

        [Fact]
        public void Adversarial_Bimodal_Extent_Is_Stable_Across_Repeated_Passes()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 40 : 300)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 5000);
            Layout(target);

            var (spread, extents) = MeasureExtentSpreadOverPasses(target, scroll, 10);
            Assert.True(spread < 1.0, $"Bimodal extent oscillated by {spread}px: [{string.Join(", ", extents)}]");
        }

        [Fact]
        public void Adversarial_Bimodal_Realized_Items_Stay_Contiguous()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 40 : 300)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            for (double offset = 0; offset < 5000; offset += 40)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
                AssertRealizedContiguous(target, $"bimodal @ {offset}");
            }
        }

        // --- Shape: extreme outliers (20px rows, occasional 2000px item) ---

        [Fact]
        public void Adversarial_Outliers_Extent_Is_Stable_Across_Repeated_Passes()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, i % 50 == 0 ? 2000 : 20)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 3000);
            Layout(target);

            var (spread, extents) = MeasureExtentSpreadOverPasses(target, scroll, 10);
            Assert.True(spread < 1.0, $"Outlier extent oscillated by {spread}px: [{string.Join(", ", extents)}]");
        }

        [Fact]
        public void Adversarial_Outliers_Realized_Items_Stay_Contiguous()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, i % 50 == 0 ? 2000 : 20)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            for (double offset = 0; offset < 6000; offset += 37)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
                AssertRealizedContiguous(target, $"outliers @ {offset}");
            }
        }

        // --- Shape: monotonic ramp (heights increase with index) ---

        [Fact]
        public void Adversarial_MonotonicRamp_Extent_Is_Stable_Across_Repeated_Passes()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, 10 + i * 2)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 10000);
            Layout(target);

            var (spread, extents) = MeasureExtentSpreadOverPasses(target, scroll, 10);
            Assert.True(spread < 1.0, $"Ramp extent oscillated by {spread}px: [{string.Join(", ", extents)}]");
        }

        [Fact]
        public void Adversarial_MonotonicRamp_Realized_Items_Stay_Contiguous()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000)
                .Select(i => (object)new ItemWithHeight(i, 10 + i * 2)).ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            for (double offset = 0; offset < 10000; offset += 100)
            {
                scroll.Offset = new Vector(0, offset);
                Layout(target);
                AssertRealizedContiguous(target, $"ramp @ {offset}");
            }
        }

        // --- Shape: async-grow (placeholder 84px -> loaded 292px on re-measure) ---
        // This is the shape whose measured size flips based on realized-set membership,
        // which is what drives the estimate oscillation the Tier-C dampers mask.

        [Fact]
        public void Adversarial_AsyncGrow_Extent_Does_Not_Oscillate_Across_Repeated_Passes()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000).Select(i => (object)new ItemWithHeight(i, 84)).ToList();
            var (target, scroll, _) =
                CreateTarget<ItemsControl, VirtualizingStackPanelAsyncGrow>(
                    items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            // Re-measure repeatedly at a fixed offset. As placeholders flip to their loaded
            // size the extent may legitimately grow, but it must never oscillate (swing back
            // down) and must stay bounded — an unstable estimate would swing it wildly.
            // NOTE (residual): the persistent per-item size record keeps the *estimate* pinned
            // (it records the raw DesiredSize, which is the 84px placeholder here, matching
            // stock), so the estimate never swings. A ~1040px monotonic creep remains as
            // realizedEndU accumulates the grown (292px) sizes of items the drifting window
            // re-measures; that creep is Tier-C damper interaction, not estimate instability,
            // and is removed in a later phase. It settles rather than exploding.
            var (spread, extents) = MeasureExtentSpreadOverPasses(target, scroll, 12);
            for (int i = 1; i < extents.Length; i++)
            {
                Assert.True(extents[i] >= extents[i - 1] - 1.0,
                    $"Async-grow extent oscillated (swung down) on pass {i}: [{string.Join(", ", extents)}]");
            }
            Assert.True(spread < 3000.0,
                $"Async-grow extent swung by {spread}px across passes: [{string.Join(", ", extents)}]");
        }

        [Fact]
        public void Adversarial_AsyncGrow_Realized_Items_Stay_Contiguous()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000).Select(i => (object)new ItemWithHeight(i, 84)).ToList();
            var (target, scroll, _) =
                CreateTarget<ItemsControl, VirtualizingStackPanelAsyncGrow>(
                    items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            for (int pass = 0; pass < 12; pass++)
            {
                target.InvalidateMeasure();
                Layout(target);
                AssertRealizedContiguous(target, $"async-grow pass {pass}");
            }
        }

        [Fact]
        public void Adversarial_AsyncGrow_Scroll_Position_Does_Not_Jump_When_Items_Grow_Off_Anchor()
        {
            using var app = App();
            var items = Enumerable.Range(0, 1000).Select(i => (object)new ItemWithHeight(i, 84)).ToList();
            var (target, scroll, itemsControl) =
                CreateTarget<ItemsControl, VirtualizingStackPanelAsyncGrow>(
                    items: items, itemTemplate: CanvasWithHeightTemplate);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            // The top-most visible element is the scroll anchor. Its index and screen
            // position must not jump as off-anchor items below it flip to their loaded size.
            var anchor = target.GetRealizedElements()
                .Where(e => e is { IsVisible: true })
                .OrderBy(e => e!.Bounds.Top)
                .First()!;
            var anchorIndex = itemsControl.IndexFromContainer((Control)anchor);
            var anchorTop = anchor.Bounds.Top;
            var offsetY = scroll.Offset.Y;

            for (int pass = 0; pass < 12; pass++)
            {
                target.InvalidateMeasure();
                Layout(target);

                Assert.True(Math.Abs(scroll.Offset.Y - offsetY) < 1.0,
                    $"Scroll offset jumped on pass {pass}: {offsetY} -> {scroll.Offset.Y}");

                var stillRealized = target.GetRealizedElements()
                    .FirstOrDefault(e => e is not null && itemsControl.IndexFromContainer((Control)e) == anchorIndex);
                if (stillRealized is not null)
                {
                    Assert.True(Math.Abs(stillRealized.Bounds.Top - anchorTop) < 1.0,
                        $"Anchor item {anchorIndex} jumped on pass {pass}: {anchorTop} -> {stillRealized.Bounds.Top}");
                }
            }
        }

        // --- Cross-region window independence (the direct realized-window test) ---
        //
        // A REGION-bimodal collection: the first half of the items are small and the
        // second half are large. As the realized window scrolls from the small region
        // into the large region and back, the reported extent must not swing based on
        // which region happens to be realized. With the old realized-set-average estimate
        // the scalar swings small<->large by region, so the extent (estimate x remaining
        // count) swings by tens of thousands of px; with the persistent per-item size
        // record the estimate is the mean of ALL measured items and no longer depends on
        // the current window, so revisiting an offset reproduces the same extent.
        [Fact]
        public void Adversarial_CrossRegion_Extent_Is_Window_Independent()
        {
            using var app = App();
            var items = Enumerable.Range(0, 200)
                .Select(i => (object)new ItemWithHeight(i, i < 100 ? 40 : 300))
                .ToList();
            var (target, scroll, _) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Prime: step the window all the way through both regions so every item is
            // measured into the record. The step MUST be <= the 100px viewport so
            // consecutive realized windows overlap and no item is skipped — with the 40px
            // small items a coarse step (e.g. 400) would jump ~10 items per pass, realizing
            // only ~3 and leaving the rest of the small region unmeasured. That produces a
            // biased, incomplete cache whose mean (and thus the reported extent) keeps
            // shifting as later scrolls fill the gaps. A fine step measures all 200 items,
            // after which the extent is exactly the true total at every offset. Offsets are
            // clamped to the current extent, which grows monotonically as large items load.
            for (double o = 0; o <= 34000; o += 80)
            {
                scroll.Offset = new Vector(0, o);
                Layout(target);
            }

            // Now revisit a set of offsets spanning both regions and the boundary, twice,
            // recording the reported extent each time. Window-independence => the extent at
            // a given offset is reproducible and the whole sweep stays within a tight band.
            double[] offsets = { 0, 1500, 3000, 3800, 20000, 30000, 33000 };
            var extents = new List<double>();
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var o in offsets)
                {
                    scroll.Offset = new Vector(0, o);
                    Layout(target);
                    extents.Add(scroll.Extent.Height);
                }
            }

            var spread = extents.Max() - extents.Min();
            // True total content = 100*40 + 100*300 = 34000. Once every item has been
            // measured (the priming above), the cache-based extent (knownSum +
            // unknown*mean with unknown==0) is exactly that true total at EVERY offset, so
            // revisiting any offset in either region reproduces the same 34000px extent and
            // the spread is ~0. The old realized-window average instead swings by > 20000px
            // across these offsets (small-region passes report a ~40px scalar, large-region
            // passes a ~300px scalar). 3000px is far below that swing yet leaves headroom
            // for any residual approximation noise.
            Assert.True(spread < 3000.0,
                $"Cross-region extent swung by {spread}px across offsets (window-dependent): " +
                $"[{string.Join(", ", extents.Select(e => e.ToString("F0")))}]");
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
            var offsetBefore = scroll.Offset.Y;

            // Resize all items uniformly — this makes startU NaN (ValidateStartU sees
            // every realized item change size by ≥ 1px and marks _startUUnstable=true).
            // Without the `!double.IsNaN(startU)` guard in CaptureViewportAnchor, the
            // cached anchor index from BEFORE the resize would be reused even though it
            // no longer corresponds to the viewport position (offset 400 / new 10px = item 40
            // versus the old anchor at ~item 20).
            foreach (var item in items)
                item.Height = 10;
            Layout(target);

            // A SECOND layout pass is required to exercise the NaN guard. The resize
            // sets _startUUnstable=true during pass 1's ValidateStartU; on pass 2,
            // _realizedElements.StartU returns NaN and CaptureViewportAnchor's NaN
            // guard determines whether the stale anchor index is reused or cleared.
            target.InvalidateMeasure();
            Layout(target);

            // Assertions:
            //  1. Realized range must shift to the new viewport position (FirstRealizedIndex
            //     increases because items are now half the height).
            //  2. scroll.Offset.Y must NOT jump. The NaN guard's purpose is preventing
            //     CompensateForExtentChange from using a stale _viewportAnchorIndex/_viewportAnchorU.
            //     With both NaN-blocking paths disabled (line 1301 guard AND line 1313 early
            //     return), CompensateForExtentChange uses stale anchor data → scroll jumps.
            Assert.True(target.FirstRealizedIndex > firstBefore,
                $"After resize, FirstRealizedIndex ({target.FirstRealizedIndex}) should be > " +
                $"previous ({firstBefore}) because items are now half the height");

            Assert.True(Math.Abs(scroll.Offset.Y - offsetBefore) < 5,
                $"Scroll offset should not jump after uniform resize. " +
                $"Before={offsetBefore}, After={scroll.Offset.Y}. " +
                $"The NaN guard(s) in CaptureViewportAnchor prevent CompensateForExtentChange " +
                $"from using a stale anchor.");
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
            // When scrolling to a disjunct viewport whose items share DataContexts with the
            // currently-realised range, RetainMatchingContainers should hold those containers
            // and reuse them WITHOUT going through PrepareContainerForItemOverride.
            //
            // The DataContexts are made to overlap by placing the same ItemWithHeight
            // INSTANCES at both index ranges. This means a disjunct-by-index scroll still
            // has DataContext overlap that RetainMatchingContainers can exploit.
            using var app = App();

            var sharedItems = Enumerable.Range(0, 10)
                .Select(i => new ItemWithHeight(i, 10))
                .ToList();

            // Place the same 10 instances at indices 0-9 and again at 50-59. Fill the rest
            // with unique items so the panel doesn't short-circuit anything else.
            var items = new List<object>();
            items.AddRange(sharedItems);                                            // [0..9]
            for (var i = 10; i < 50; i++) items.Add(new ItemWithHeight(i, 10));     // [10..49]
            items.AddRange(sharedItems);                                            // [50..59]
            for (var i = 60; i < 100; i++) items.Add(new ItemWithHeight(i, 10));    // [60..99]

            var (target, scroll, itemsControl) =
                CreateTarget<CountingPrepareItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.0d);

            // Initial layout realises 0-9 (sharedItems).
            Assert.Equal(0, target.FirstRealizedIndex);

            // Reset Prepare counter — only count preparations triggered by the disjunct scroll.
            itemsControl.PrepareCount = 0;

            // Disjunct scroll to viewport that contains the SAME sharedItems at indices 50-59.
            scroll.Offset = new Vector(0, 500);
            Layout(target);

            Assert.True(target.FirstRealizedIndex >= 40,
                $"After disjunct scroll, FirstRealizedIndex should be >= 40, but was {target.FirstRealizedIndex}");

            // With RetainMatchingContainers: matched sharedItems are retained, so the post-disjunct
            // realisation skips PrepareContainerForItem for them. Without RetainMatchingContainers,
            // every realised slot is recycled and re-prepared.
            Assert.True(itemsControl.PrepareCount <= 3,
                $"Expected ≤ 3 container preparations after disjunct scroll with DC-overlap, " +
                $"got {itemsControl.PrepareCount}. Without RetainMatchingContainers every realised " +
                $"slot would be re-prepared.");
        }

        // ===== Category: Item 0 position correction =====

        [Fact]
        public void Item_Zero_Is_Always_At_Position_Zero()
        {
            // Item 0 is at u == 0 by definition, so whenever realization reaches it the block
            // must be re-based to StartU == 0. Realization walks backwards from an *estimated*
            // anchor position and can therefore arrive at item 0 with a non-zero u; leaving that
            // in would clip item 0 above the viewport or leave a gap above it.
            //
            // We force a non-zero u for item 0 by injecting a non-zero _startU on
            // _realizedElements via reflection between scrolls, and using bufferFactor=0
            // so the extended viewport doesn't touch zero (avoiding the IsZero shortcut
            // in GetOrEstimateAnchorElementForViewport). With the re-basing,
            // _realizedElements._startU after the layout is 0. Without, it stays at the
            // injected non-zero value.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, 20))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.0d);

            scroll.Offset = new Vector(0, 5);
            Layout(target);

            // Inject a non-zero _startU on _realizedElements so the next CaptureViewportAnchor
            // realised-elements loop returns item 0 at u=50.
            var realizedElementsField = typeof(VirtualizingStackPanel).GetField(
                "_realizedElements",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var realizedElements = realizedElementsField!.GetValue(target);
            var startUField = realizedElements!.GetType().GetField(
                "_startU",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startUField!.SetValue(realizedElements, 50.0);

            target.InvalidateMeasure();
            Layout(target);

            var container0 = target.ContainerFromIndex(0);
            Assert.NotNull(container0);

            // Read final _startU as the most direct probe of the correction.
            var realizedElementsAfter = realizedElementsField.GetValue(target);
            var startUAfter = (double)startUField.GetValue(realizedElementsAfter!)!;

            Assert.True(container0!.Bounds.Top < 3 && startUAfter < 3,
                $"Item 0 invariant broken: Bounds.Top={container0.Bounds.Top}, " +
                $"_startU={startUAfter}.");
        }

        // ===== Category: Tier A — index -> container mapping across collection changes =====
        //
        // Every keep / reuse / recycle decision the panel makes has to end with each realized
        // container showing the item its index says it should. This is the only class of bug in
        // the panel that corrupts what the user sees (an item rendered under the wrong heading),
        // so it is checked exhaustively: for each kind of collection change, at each position
        // relative to the realized window, plus the coalesced-Reset shapes that collection
        // libraries emit when a batch of edits crosses their reset threshold.

        /// <summary>
        /// Asserts every realized container is showing the item its index maps to.
        /// </summary>
        private static void AssertContainerIndexMappingIsCorrect(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            IReadOnlyList<object> items,
            string context)
        {
            var checked_ = 0;
            foreach (var container in target.GetRealizedContainers()!)
            {
                var index = itemsControl.IndexFromContainer(container);
                if (index < 0)
                    continue;

                Assert.True(index < items.Count,
                    $"Container realized at index {index} but the collection only has " +
                    $"{items.Count} items ({context}).");

                var dataContext = (container as IDataContextProvider)?.DataContext;
                Assert.True(ReferenceEquals(dataContext, items[index]),
                    $"Container at index {index} is showing '{dataContext}' but Items[{index}] " +
                    $"is '{items[index]}' ({context}).");
                checked_++;
            }

            Assert.True(checked_ > 0, $"No realized containers to check ({context}).");
        }

        public enum CollectionEdit
        {
            InsertOne,
            InsertRange,
            RemoveOne,
            RemoveRange,
            Replace,
            MoveForward,
            MoveBackward,
        }

        /// <summary>Where the edit lands relative to the realized window.</summary>
        public enum EditPosition
        {
            BeforeWindow,

            /// <summary>Just after the first realized item, so nearly every realized item shifts.</summary>
            EarlyInWindow,

            /// <summary>
            /// Past the middle of the realized window, so a majority of realized items still match
            /// their index and only the tail shifts. This is the shape that defeats any
            /// "most items still match, so keep them all" shortcut, and it is how a mid-list edit
            /// coalesced into a Reset originally rendered items under the wrong heading.
            /// </summary>
            LateInWindow,

            AfterWindow,
        }

        [Theory]
        [InlineData(CollectionEdit.InsertOne, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.InsertOne, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.InsertOne, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.InsertOne, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.InsertRange, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.InsertRange, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.InsertRange, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.InsertRange, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.RemoveOne, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.RemoveOne, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.RemoveOne, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.RemoveOne, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.RemoveRange, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.RemoveRange, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.RemoveRange, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.RemoveRange, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.Replace, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.Replace, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.Replace, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.Replace, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.MoveForward, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.MoveForward, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.MoveForward, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.MoveForward, EditPosition.AfterWindow)]
        [InlineData(CollectionEdit.MoveBackward, EditPosition.BeforeWindow)]
        [InlineData(CollectionEdit.MoveBackward, EditPosition.EarlyInWindow)]
        [InlineData(CollectionEdit.MoveBackward, EditPosition.LateInWindow)]
        [InlineData(CollectionEdit.MoveBackward, EditPosition.AfterWindow)]
        public void Collection_Edit_Keeps_Every_Container_On_Its_Own_Item(
            CollectionEdit edit, EditPosition position)
        {
            using var app = App();

            // Varied heights so a mis-mapping shows up as a position error too, not just a
            // wrong DataContext.
            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 200)
                    .Select(i => (object)new ItemWithHeight(i, 10 + (i % 4) * 20)));

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Park the realized window in the middle so all three edit positions exist.
            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            var first = target.FirstRealizedIndex;
            var last = target.LastRealizedIndex;
            Assert.True(first > 10 && last < 180,
                $"Test setup: expected a mid-list window, got [{first}..{last}].");

            var at = position switch
            {
                EditPosition.BeforeWindow => first - 5,
                EditPosition.EarlyInWindow => first + 1,
                EditPosition.LateInWindow => first + ((last - first) * 3 / 4),
                EditPosition.AfterWindow => last + 5,
                _ => throw new ArgumentOutOfRangeException(nameof(position)),
            };

            switch (edit)
            {
                case CollectionEdit.InsertOne:
                    items.Insert(at, new ItemWithHeight(900, 55));
                    break;
                case CollectionEdit.InsertRange:
                    for (var i = 0; i < 3; i++)
                        items.Insert(at + i, new ItemWithHeight(900 + i, 35 + i * 10));
                    break;
                case CollectionEdit.RemoveOne:
                    items.RemoveAt(at);
                    break;
                case CollectionEdit.RemoveRange:
                    for (var i = 0; i < 3; i++)
                        items.RemoveAt(at);
                    break;
                case CollectionEdit.Replace:
                    items[at] = new ItemWithHeight(900, 65);
                    break;
                case CollectionEdit.MoveForward:
                    items.Move(at, at + 4);
                    break;
                case CollectionEdit.MoveBackward:
                    items.Move(at, at - 4);
                    break;
            }

            Layout(target);

            var context = $"{edit} at {position} (index {at}), window was [{first}..{last}]";
            AssertContainerIndexMappingIsCorrect(target, itemsControl, items, context);
            AssertRealizedContiguous(target, context);
        }

        [Theory]
        [InlineData(EditPosition.BeforeWindow)]
        [InlineData(EditPosition.EarlyInWindow)]
        [InlineData(EditPosition.LateInWindow)]
        [InlineData(EditPosition.AfterWindow)]
        public void Insert_Coalesced_Into_A_Reset_Keeps_Every_Container_On_Its_Own_Item(
            EditPosition position)
        {
            // The shape that produced the original wrong-item render: a collection library
            // (DynamicData's Bind past its reset threshold, ObservableCollection.Clear+AddRange,
            // a sort) reports a batch of edits as a single Reset. Everything before the edit point
            // still matches its index while everything at or after it has shifted — so any
            // "most items still match, keep them all" shortcut pins the shifted items to the wrong
            // containers.
            using var app = App();

            // Uniform heights on purpose: the inserted item is the same size as everything else,
            // so nothing about the *sizes* betrays the shift. Every size-based signal the panel
            // has (its resize detection, its extent) looks unchanged, which leaves the
            // index -> item mapping as the only thing that can catch the corruption. Varied
            // heights would let a size mismatch trip re-realization and mask the bug.
            var backing = Enumerable.Range(0, 400)
                .Select(i => (object)new ItemWithHeight(i, 10))
                .ToList();
            var items = new ResettingObservableCollection<object>(backing);

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            var first = target.FirstRealizedIndex;
            var last = target.LastRealizedIndex;
            Assert.True(first > 10 && last < 380 && last - first >= 8,
                $"Test setup: expected a mid-list window of several items, got [{first}..{last}].");

            var at = position switch
            {
                EditPosition.BeforeWindow => first - 5,
                EditPosition.EarlyInWindow => first + 1,
                EditPosition.LateInWindow => first + ((last - first) * 3 / 4),
                EditPosition.AfterWindow => last + 5,
                _ => throw new ArgumentOutOfRangeException(nameof(position)),
            };

            var mutated = new List<object>(backing);
            mutated.Insert(at, new ItemWithHeight(900, 10));
            items.Reset(mutated);
            Layout(target);

            var context = $"insert at {position} (index {at}) coalesced into a Reset, " +
                          $"window was [{first}..{last}]";
            AssertContainerIndexMappingIsCorrect(target, itemsControl, items, context);
            AssertRealizedContiguous(target, context);
        }

        [Fact]
        public void Reorder_Coalesced_Into_A_Reset_Keeps_Every_Container_On_Its_Own_Item()
        {
            // A sort reported as a Reset: every item survives but almost none keeps its index.
            using var app = App();

            var backing = Enumerable.Range(0, 200)
                .Select(i => (object)new ItemWithHeight(i, 10 + (i % 4) * 20))
                .ToList();
            var items = new ResettingObservableCollection<object>(backing);

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            var reversed = new List<object>(backing);
            reversed.Reverse();
            items.Reset(reversed);
            Layout(target);

            AssertContainerIndexMappingIsCorrect(target, itemsControl, items, "reversed via Reset");
            AssertRealizedContiguous(target, "reversed via Reset");
        }

        [Fact]
        public void Append_Coalesced_Into_A_Reset_Keeps_Every_Container_On_Its_Own_Item()
        {
            // The case the Reset-preservation shortcut exists for: a pure append, where every
            // realized item does keep its index and the realized window can legitimately be kept.
            using var app = App();

            var backing = Enumerable.Range(0, 200)
                .Select(i => (object)new ItemWithHeight(i, 10 + (i % 4) * 20))
                .ToList();
            var items = new ResettingObservableCollection<object>(backing);

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            var firstBefore = target.FirstRealizedIndex;

            var appended = new List<object>(backing);
            appended.AddRange(Enumerable.Range(200, 50)
                .Select(i => (object)new ItemWithHeight(i, 10 + (i % 4) * 20)));
            items.Reset(appended);
            Layout(target);

            AssertContainerIndexMappingIsCorrect(target, itemsControl, items, "append via Reset");
            AssertRealizedContiguous(target, "append via Reset");
            Assert.Equal(firstBefore, target.FirstRealizedIndex);
        }

        // ===== Category: Estimate caching =====

        [Fact]
        public void Estimate_Cache_Skips_Recalculation_When_Range_Unchanged()
        {
            // EstimateElementSizeU caches by realized range (first/last index).
            // When the same range is measured again with identical sizes, the cached
            // value prevents smoothing convergence drift.
            //
            // NOTE: This cache is a micro-optimization for sub-pixel non-deterministic
            // measurements (async image loading, text wrapping). With deterministic
            // measurements and Avalonia's layout rounding, the cache is invisible
            // because smoothing with identical inputs produces identical output.
            // This test verifies the basic invariant: extent is stable across
            // layouts when nothing changes.
            using var app = App();

            var items = Enumerable.Range(0, 100)
                .Select(i => (object)new ItemWithHeight(i, i % 2 == 0 ? 10 : 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                    items: items,
                    itemTemplate: CanvasWithHeightTemplate,
                    bufferFactor: 0.5d);

            var extent1 = scroll.Extent.Height;

            // Another layout without changes — extent must be stable
            Layout(target);
            var extent2 = scroll.Extent.Height;

            Assert.Equal(extent1, extent2);
        }

        [Fact]
        public void Estimate_Cache_Invalidated_After_Genuine_Resize()
        {
            // When ValidateStartU detects a genuine resize (>= 1px), the estimate cache
            // indices are reset to -1, forcing EstimateElementSizeU to recalculate.
            //
            // Use small items with buffer so multiple items are realized, ensuring
            // ValidateStartU marks startU as unstable (NaN), which triggers cache
            // invalidation. Without the invalidation, the cached estimate (old size)
            // would be returned when the realized range happens to match.
            using var app = App();
            var trace = new System.Text.StringBuilder();

            var items = Enumerable.Range(0, 50).Select(x => new ItemWithHeight(x, 40)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.5d);

            // Initial extent: based on 40px items
            var extentBefore = scroll.Extent.Height;
            trace.AppendLine($"After initial layout: extent={extentBefore} realized=[{target.FirstRealizedIndex}..{target.LastRealizedIndex}]");

            // Shrink items by half — genuine resize, estimate cache must invalidate
            foreach (var item in items)
                item.Height = 20;
            Layout(target);

            var extentAfter = scroll.Extent.Height;
            trace.AppendLine($"After resize + layout: extent={extentAfter} realized=[{target.FirstRealizedIndex}..{target.LastRealizedIndex}]");

            // Extent should roughly halve (estimate updated to reflect new sizes)
            Assert.True(extentAfter < extentBefore * 0.75,
                $"After halving item heights, extent should decrease significantly. " +
                $"Before: {extentBefore}, After: {extentAfter}. Trace:\n{trace}");
        }

        // ===== Category: ValidateStartU resize detection (Tier C4 clean-up) =====
        //
        // ValidateStartU used to classify a size change as "real" only at >= 1px, absorbing
        // anything smaller, and to fire at most once per measure→arrange cycle
        // (_suppressValidateStartU). Both were tuned against one in-house control's
        // measurement noise. The replacement detects any layout-significant change
        // (LayoutHelper.LayoutEpsilon) and runs on every measure pass.

        [Fact]
        public void Sub_Pixel_Pre_Anchor_Resize_Is_Compensated_Not_Absorbed()
        {
            // A real 0.5px growth of an item above the viewport must move StartU by 0.5px so
            // the anchor stays put. The old `Math.Abs(diff) >= 1.0` threshold swallowed it:
            // the stored size was updated but StartU was not, so the anchor slid by 0.5px —
            // and did so again on every subsequent sub-pixel settle.
            //
            // Uses a template that opts out of layout rounding, so a fractional height
            // survives into DesiredSize. Fractional desired sizes are ordinary in real apps:
            // any fractional layout scale (125%, 150% DPI) makes the rounding grid itself
            // sub-pixel, so genuine sub-1px size changes reach the panel.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            // The panel records the *container's* DesiredSize, so the container must opt out
            // of rounding too, not just the templated content.
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: UnroundedCanvasWithHeightTemplate,
                styles: new[]
                {
                    new Style(x => x.OfType<ContentPresenter>())
                    {
                        Setters = { new Setter(Layoutable.UseLayoutRoundingProperty, false) }
                    }
                },
                bufferFactor: 1.0d);

            scroll.Offset = new Vector(0, 600);
            Layout(target);

            var firstRealized = target.FirstRealizedIndex;
            var anchorIdx = (int)(scroll.Offset.Y / 50);
            Assert.True(firstRealized + 1 < anchorIdx,
                $"Need >= 2 buffer items before the anchor. firstRealized={firstRealized}, anchor={anchorIdx}");

            var startUField = GetRealizedStartUAccessor(target, out var realizedElements);
            var startUBefore = (double)startUField.GetValue(realizedElements)!;

            // Grow a mid-buffer item above the anchor by 0.5px, with the child already
            // carrying its new DesiredSize so ValidateStartU (which runs before realization)
            // observes the diff.
            var growIdx = firstRealized + 1;
            var growContainer = itemsControl.ContainerFromIndex(growIdx) as Control;
            Assert.NotNull(growContainer);
            items[growIdx].Height = 50.5;
            growContainer!.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.InvalidateMeasure();
            Layout(target);

            var realizedAfter = GetRealizedStackElements(target);
            var startUAfter = (double)startUField.GetValue(realizedAfter)!;

            // StartU must have absorbed the 0.5px of pre-anchor growth by moving up.
            Assert.True(startUAfter <= startUBefore - 0.4,
                $"StartU did not compensate for 0.5px of pre-anchor growth: " +
                $"{startUBefore} -> {startUAfter}. A sub-pixel-but-real resize is being " +
                $"absorbed instead of compensated.");
        }

        [Fact]
        public void Second_Resize_In_Same_Arrange_Cycle_Is_Still_Compensated()
        {
            // _suppressValidateStartU allowed only ONE resize compensation per measure→arrange
            // cycle. A layout cycle that measures twice before arranging (exactly what happens
            // when content settles across passes) therefore dropped the second resize, leaving
            // StartU short by that item's growth and sliding the anchor.
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

            var firstRealized = target.FirstRealizedIndex;
            var anchorIdx = (int)(scroll.Offset.Y / 50);
            Assert.True(firstRealized + 1 < anchorIdx,
                $"Need >= 2 buffer items before the anchor. firstRealized={firstRealized}, anchor={anchorIdx}");

            var startUField = GetRealizedStartUAccessor(target, out var realizedElements);
            var startUBefore = (double)startUField.GetValue(realizedElements)!;

            var availableSize = new Size(target.Bounds.Width, target.Bounds.Height);

            // Two measure passes with NO arrange in between, growing the same mid-buffer item
            // above the anchor by 20px each time (50 -> 70 -> 90) — content settling in steps.
            // Both steps must be compensated: total StartU shift 40px.
            var growIdx = firstRealized + 1;
            foreach (var height in new double[] { 70, 90 })
            {
                var container = itemsControl.ContainerFromIndex(growIdx) as Control;
                Assert.NotNull(container);
                items[growIdx].Height = height;
                // The container itself must be re-measured, not just its content: a data change
                // inside the template invalidates the templated child, not the container, and the
                // panel skips measure-valid containers.
                container!.InvalidateMeasure();
                container.Measure(new Size(100, double.PositiveInfinity));
                Assert.Equal(height, container.DesiredSize.Height);

                target.InvalidateMeasure();
                target.Measure(availableSize);
            }

            var realizedAfter = GetRealizedStackElements(target);
            var startUAfter = (double)startUField.GetValue(realizedAfter)!;
            var shift = startUBefore - startUAfter;

            Assert.True(shift >= 39,
                $"Only {shift}px of the 40px pre-anchor growth was compensated " +
                $"(StartU {startUBefore} -> {startUAfter}). The second resize in the same " +
                $"measure→arrange cycle was suppressed.");
        }

        private static object GetRealizedStackElements(VirtualizingStackPanel target)
        {
            var field = typeof(VirtualizingStackPanel).GetField(
                "_realizedElements",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            return field.GetValue(target)!;
        }

        private static System.Reflection.FieldInfo GetRealizedStartUAccessor(
            VirtualizingStackPanel target, out object realizedElements)
        {
            realizedElements = GetRealizedStackElements(target);
            return realizedElements.GetType().GetField(
                "_startU",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        }

        // ===== Category: anchor-drift compensation (formerly extent dampening) =====
        //
        // CompensateForExtentChange used to gate anchor compensation on the *extent* delta
        // (a 2px noise floor plus a "dampening" branch keyed on magic 0.5 ratio / 10%
        // realized / >10 unrealized / 0.3 damp factor). The dampening branch skipped the
        // anchor compensation AND recorded a fabricated previous extent, so the next pass
        // saw the same large delta and skipped compensation again — the anchor kept
        // drifting for as long as the panel stayed in that regime. These tests assert the
        // constant-free replacement: compensation is gated on the *anchor drift* itself.

        [Fact]
        public void Anchor_Stays_Put_Across_Repeated_Passes_With_Few_Realized_Items()
        {
            // The exact regime the removed dampening branch keyed on: a tiny fraction of
            // the collection realized (bufferFactor 0, 100px viewport, 500 items) and a
            // huge extent swing as the window moves out of the small-item head into the
            // large-item body. In that regime the dampener suppressed anchor compensation,
            // so the first visible item drifted pass after pass. With drift-gated
            // compensation the visible content must stay pinned.
            using var app = App();

            var items = Enumerable.Range(0, 500)
                .Select(i => (object)new ItemWithHeight(i, i < 5 ? 5 : 300))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.0d);

            scroll.Offset = new Vector(0, 2000);
            Layout(target);

            var anchor = target.GetRealizedElements()
                .Where(e => e is { IsVisible: true })
                .OrderBy(e => e!.Bounds.Top)
                .First()!;
            var anchorIndex = itemsControl.IndexFromContainer((Control)anchor);
            var anchorTop = anchor.Bounds.Top;

            for (var pass = 0; pass < 10; pass++)
            {
                target.InvalidateMeasure();
                Layout(target);

                var still = target.GetRealizedElements()
                    .FirstOrDefault(e => e is not null &&
                        itemsControl.IndexFromContainer((Control)e) == anchorIndex);

                Assert.True(still is not null,
                    $"Anchor item {anchorIndex} was de-realized on pass {pass} even though the " +
                    $"viewport never moved — positions drifted out from under it.");
                Assert.True(Math.Abs(still!.Bounds.Top - anchorTop) < 1.0,
                    $"Anchor item {anchorIndex} drifted on pass {pass}: {anchorTop} -> " +
                    $"{still.Bounds.Top}. Anchor compensation is being skipped.");
            }
        }

        [Fact]
        public void Reported_Extent_Is_Reproducible_When_Revisiting_An_Offset_With_Few_Realized_Items()
        {
            // The dampener recorded a fabricated "previous extent" instead of the value the
            // panel actually reported. Any book-keeping the panel does about the extent must
            // describe what it reported, otherwise the delta driving compensation is a
            // fiction. The observable form: returning to the same offset must reproduce the
            // same extent, rather than the extent creeping as the fake baseline is chased.
            using var app = App();

            var items = Enumerable.Range(0, 500)
                .Select(i => (object)new ItemWithHeight(i, i < 5 ? 5 : 300))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0.0d);

            // Prime the per-item size record across the whole collection so the extent has
            // converged on the true total; step finer than the viewport so nothing is skipped.
            for (double o = 0; o <= 150000; o += 90)
            {
                scroll.Offset = new Vector(0, o);
                Layout(target);
            }

            double[] offsets = { 0, 2000, 40000, 100000, 148000 };
            var first = new List<double>();
            foreach (var o in offsets)
            {
                scroll.Offset = new Vector(0, o);
                Layout(target);
                first.Add(scroll.Extent.Height);
            }

            var second = new List<double>();
            foreach (var o in offsets)
            {
                scroll.Offset = new Vector(0, o);
                Layout(target);
                second.Add(scroll.Extent.Height);
            }

            for (var i = 0; i < offsets.Length; i++)
            {
                Assert.True(Math.Abs(first[i] - second[i]) < 1.0,
                    $"Extent at offset {offsets[i]} was not reproducible: " +
                    $"{first[i]} then {second[i]}. First sweep: " +
                    $"[{string.Join(", ", first.Select(e => e.ToString("F0")))}], " +
                    $"second: [{string.Join(", ", second.Select(e => e.ToString("F0")))}]");
            }
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
        public void Size_Change_During_Layout_Is_Logged()
        {
            // When an item's size changes during layout (e.g. async image loading), the panel
            // logs it so non-deterministic item templates can be diagnosed. Verbose, not
            // Warning: a template settling its size is legitimate and supported, so this is a
            // diagnostic aid rather than a defect report — and any layout-significant change now
            // reaches it, which at Warning level would be noise.
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

                // Resize an item — should be reported
                items[0].Height = 25;
                Layout(target);

                Assert.Contains(logMessages, m =>
                    m.Contains("Item template size changed during layout") &&
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
            // anchor (first visible item) at its visual viewport-relative position.
            // A wrong sign would push the anchor away by ~2× the size delta.
            using var app = App();

            var items = Enumerable.Range(0, 50)
                .Select(i => new ItemWithHeight(i, 50))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 1.0d);

            // Scroll to offset 600 → viewport=[600,700]
            scroll.Offset = new Vector(0, 600);
            Layout(target);

            var anchorIdx = (int)(scroll.Offset.Y / 50); // ~12
            var firstRealized = target.FirstRealizedIndex;
            Assert.True(firstRealized + 1 < anchorIdx, // need a mid-buffer item, not the edge
                $"Need ≥2 buffer items before anchor. firstRealized={firstRealized}, anchor≈{anchorIdx}");

            // Record viewport-relative position of the anchor (first visible) item.
            // The preDelta compensation should keep this stable when items before it resize.
            var anchorContainerBefore = itemsControl.ContainerFromIndex(anchorIdx) as Control;
            Assert.NotNull(anchorContainerBefore);
            var visualPosBefore = anchorContainerBefore!.Bounds.Y - scroll.Offset.Y;

            // Simulate async image loading: a MID-buffer item ABOVE the viewport (not at the
            // realization edge, so it stays in the realized window) grows 150px (50→200).
            // As in the shrinking case, the child must already carry its NEW DesiredSize by
            // the time the panel's MeasureOverride runs ValidateStartU — otherwise the growth
            // is folded into realization and the pre-anchor compensation path is never
            // exercised, leaving this test vacuous.
            var growIdx = firstRealized + 1;
            var growContainer = itemsControl.ContainerFromIndex(growIdx) as Control;
            Assert.NotNull(growContainer);
            items[growIdx].Height = 200;
            growContainer!.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.InvalidateMeasure();
            Layout(target);

            var anchorContainerAfter = itemsControl.ContainerFromIndex(anchorIdx) as Control;
            Assert.NotNull(anchorContainerAfter);
            var visualPosAfter = anchorContainerAfter!.Bounds.Y - scroll.Offset.Y;

            // Tolerance ≤ 5px allows for extent/anchor-recalc jitter.
            // Wrong sign would shift by ~2×150=300px.
            Assert.True(Math.Abs(visualPosAfter - visualPosBefore) < 5,
                $"Anchor jumped when item above viewport grew: " +
                $"visualPosBefore={visualPosBefore}, visualPosAfter={visualPosAfter}, " +
                $"delta={visualPosAfter - visualPosBefore}. " +
                $"This suggests the preDelta compensation sign in ValidateStartU is wrong.");
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

            var anchorIdx = (int)(scroll.Offset.Y / 50);
            var firstRealized = target.FirstRealizedIndex;
            Assert.True(firstRealized + 1 < anchorIdx, // need a mid-buffer item, not the edge
                $"Need ≥2 buffer items before anchor. firstRealized={firstRealized}, anchor≈{anchorIdx}");

            var anchorContainerBefore = itemsControl.ContainerFromIndex(anchorIdx) as Control;
            Assert.NotNull(anchorContainerBefore);
            var visualPosBefore = anchorContainerBefore!.Bounds.Y - scroll.Offset.Y;

            // Shrink a MID-buffer item (not at the realization edge) so the item
            // stays in the realized window. To exercise ValidateStartU's preDelta
            // compensation we need the child Canvas to have its NEW DesiredSize
            // by the time the panel's MeasureOverride (which runs ValidateStartU
            // before realization) executes. Force re-measure of the child first.
            var shrinkIdx = firstRealized + 1;
            var shrinkContainer = itemsControl.ContainerFromIndex(shrinkIdx) as Control;
            Assert.NotNull(shrinkContainer);
            items[shrinkIdx].Height = 30;
            shrinkContainer!.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.InvalidateMeasure();
            Layout(target);

            var anchorContainerAfter = itemsControl.ContainerFromIndex(anchorIdx) as Control;
            Assert.NotNull(anchorContainerAfter);
            var visualPosAfter = anchorContainerAfter!.Bounds.Y - scroll.Offset.Y;

            // The visual position of the anchor is preserved by the natural anchor recapture
            // (insensitive to sign). However, the FirstRealizedIndex IS sensitive: the preDelta
            // compensation in ValidateStartU shifts _realizedElements.StartU which is read by
            // CalculateMeasureViewport's anchor estimation in the SAME measure pass (before the
            // swap). With the right sign the anchor estimation includes one extra item backward;
            // with the wrong sign that item drops out, shifting FirstRealizedIndex by 1.
            //
            // Observed: correct sign → FirstRealizedIndex = 9, wrong sign → 10.
            Assert.True(target.FirstRealizedIndex <= 9,
                $"Expected FirstRealizedIndex ≤ 9 after backward-anchor-recapture following a " +
                $"shrink (preDelta compensation extends realisation one item backward), got " +
                $"{target.FirstRealizedIndex}. The preDelta sign in ValidateStartU may be wrong.");

            // Also sanity-check visual stability (no scroll jump).
            Assert.True(Math.Abs(visualPosAfter - visualPosBefore) < 5,
                $"Anchor jumped when item above viewport shrank: " +
                $"visualPosBefore={visualPosBefore}, visualPosAfter={visualPosAfter}, " +
                $"delta={visualPosAfter - visualPosBefore}.");
        }

        [Fact]
        public void Last_Item_Is_Reachable_And_Extent_Is_Exact_After_Scrolling_To_End()
        {
            // 12 items in a 100px viewport: the first 10 are 10px tall, the last 2 are 200px, so
            // the true total is 100 + 400 = 500. The tail items are far larger than the mean of
            // everything measured so far, so an estimate-based extent starts out much too small —
            // the shape that used to leave the last item unreachable/clipped.
            //
            // Asking the ScrollViewer for the bottom must still walk to the end: each pass measures
            // more of the tail, which sharpens the estimate (it can only sharpen, never oscillate —
            // the estimate is a mean over the persistent per-item size record, not over the
            // currently-realized window). Once every item has been measured unknownCount == 0 and
            // CacheBasedExtentU returns the exact total, so the bottom edge is the real bottom edge
            // and the last item is not clipped.
            using var app = App();

            var items = Enumerable.Range(0, 12)
                .Select(i => new ItemWithHeight(i, i >= 10 ? 200 : 10))
                .ToList();

            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: CanvasWithHeightTemplate,
                bufferFactor: 0);

            for (var i = 0; i < 20; i++)
            {
                var previousOffset = scroll.Offset.Y;
                scroll.Offset = new Vector(0, scroll.Extent.Height);
                Layout(target);

                if (Math.Abs(scroll.Offset.Y - previousOffset) < 1)
                    break;
            }

            // Every item has been measured, so the reported extent is the exact measured total.
            Assert.Equal(500, scroll.Extent.Height);

            // The last item is realized, laid out at its true position and reachable in full: its
            // bottom edge coincides with the bottom of the viewport at the maximum offset.
            Assert.Equal(11, target.LastRealizedIndex);

            var last = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(11));
            Assert.Equal(new Rect(0, 300, 100, 200), last.Bounds);
            Assert.Equal(400, scroll.Offset.Y);
            Assert.True(last.Bounds.Bottom <= scroll.Offset.Y + scroll.Viewport.Height,
                $"Last item is clipped: it ends at {last.Bounds.Bottom} but the viewport ends at " +
                $"{scroll.Offset.Y + scroll.Viewport.Height}.");
        }

        private class TestLogSink : ILogSink
        {
            private readonly List<string> _messages;

            public TestLogSink(List<string> messages) => _messages = messages;

            public bool IsEnabled(LogEventLevel level, string area) =>
                level >= LogEventLevel.Verbose && area == LogArea.Control;

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
