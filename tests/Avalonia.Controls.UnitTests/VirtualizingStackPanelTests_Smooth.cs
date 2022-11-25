using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class VirtualizingStackPanelTests_Smooth
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
        public void Creates_Elements_For_Inserted_Item()
        {
            using var app = App();

            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.Items;

            Assert.Equal(10, target.GetRealizedElements().Count);

            items.Insert(2, "new");

            Assert.Equal(11, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent row indexes updated.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new row is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new row and the removed last row, all existing elements should be the same.
            elements[2] = target.GetRealizedElements().ElementAt(2);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Updates_Elements_On_Removed_Row()
        {
            using var app = App();

            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.Items;

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

        private static IReadOnlyList<int> GetRealizedIndexes(VirtualizingStackPanel target, ItemsControl itemsControl)
        {
            return target.GetRealizedElements()
                .Select(x => x is null ? -1 : itemsControl.ItemContainerGenerator.IndexFromContainer((Control)x))
                .ToList();
        }

        private static void AssertRealizedItems(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            int firstIndex,
            int count)
        {
            var childIndexes = target.GetLogicalChildren()
                .Select(x => itemsControl.ItemContainerGenerator.IndexFromContainer((Control)x))
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(Enumerable.Range(firstIndex, count), childIndexes);
        }

        private static (VirtualizingStackPanel, ScrollViewer, ItemsControl) CreateTarget(int itemCount = 100)
        {
            var items = new ObservableCollection<string>(Enumerable.Range(0, itemCount).Select(x => $"Item {x}"));
            var target = new VirtualizingStackPanel();
            
            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            var scroll = new ScrollViewer 
            { 
                Content = presenter,
                Template = ScrollViewerTemplate(),
            };

            var itemsControl = new ItemsControl
            {
                Items = items,
                Template = new FuncControlTemplate<ItemsControl>((_, _) => scroll),
                ItemsPanel = new FuncTemplate<Panel>(() => target),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new Canvas { Width = 100, Height = 10 }),
            };

            var root = new TestRoot(true, itemsControl);
            root.ClientSize = new(100, 100);
            root.LayoutManager.ExecuteInitialLayoutPass();

            return (target, scroll, itemsControl);
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
                    [~ContentPresenter.ContentProperty] = x[~ContentControl.ContentProperty],
                    [~~ScrollContentPresenter.ExtentProperty] = x[~~ScrollViewer.ExtentProperty],
                    [~~ScrollContentPresenter.OffsetProperty] = x[~~ScrollViewer.OffsetProperty],
                    [~~ScrollContentPresenter.ViewportProperty] = x[~~ScrollViewer.ViewportProperty],
                    [~ScrollContentPresenter.CanHorizontallyScrollProperty] = x[~ScrollViewer.CanHorizontallyScrollProperty],
                    [~ScrollContentPresenter.CanVerticallyScrollProperty] = x[~ScrollViewer.CanVerticallyScrollProperty],
                }.RegisterInNameScope(ns));
        }

        private static IDisposable App() => UnitTestApplication.Start();
    }
}
