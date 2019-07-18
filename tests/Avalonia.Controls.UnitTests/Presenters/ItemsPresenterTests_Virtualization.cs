// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ItemsPresenterTests_Virtualization
    {
        [Fact]
        public void Should_Not_Create_Items_Before_Added_To_Visual_Tree()
        {
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var target = new TestItemsPresenter(true)
            {
                Items = items,
                ItemsPanel = VirtualizingPanelTemplate(Orientation.Vertical),
                ItemTemplate = ItemTemplate(),
                VirtualizationMode = ItemVirtualizationMode.Simple,
            };

            var scroller = new ScrollContentPresenter
            {
                Content = target,
            };

            scroller.UpdateChild();
            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Empty(target.Panel.Children);

            var root = new TestRoot
            {
                Child = scroller,
            };

            target.InvalidateMeasure();
            target.Panel.InvalidateMeasure();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_False_When_Has_No_Virtualizing_Panel()
        {
            var target = CreateTarget();
            target.ClearValue(ItemsPresenter.ItemsPanelProperty);

            target.ApplyTemplate();

            Assert.False(((ILogicalScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_False_When_VirtualizationMode_None()
        {
            var target = CreateTarget(ItemVirtualizationMode.None);

            target.ApplyTemplate();

            Assert.False(((ILogicalScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_False_When_Doesnt_Have_ScrollPresenter_Parent()
        {
            var target = new ItemsPresenter
            {
                ItemsPanel = VirtualizingPanelTemplate(),
                ItemTemplate = ItemTemplate(),
                VirtualizationMode = ItemVirtualizationMode.Simple,
            };

            target.ApplyTemplate();

            Assert.False(((ILogicalScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_True()
        {
            var target = CreateTarget();

            target.ApplyTemplate();

            Assert.True(((ILogicalScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Parent_ScrollContentPresenter_Properties_Should_Be_Set()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var scroll = (ScrollContentPresenter)target.Parent;
            Assert.Equal(new Size(10, 20), scroll.Extent);
            Assert.Equal(new Size(100, 10), scroll.Viewport);
        }

        [Fact]
        public void Should_Fill_Panel_With_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();

            target.Measure(new Size(100, 100));
            Assert.Equal(10, target.Panel.Children.Count);

            target.Arrange(new Rect(0, 0, 100, 100));
            Assert.Equal(10, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Only_Create_Enough_Containers_To_Display_All_Items()
        {
            var target = CreateTarget(itemCount: 2);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(2, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Expand_To_Fit_Containers_When_Flexible_Size()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(10, 200), target.DesiredSize);
            Assert.Equal(new Size(10, 200), target.Bounds.Size);
            Assert.Equal(20, target.Panel.Children.Count);
        }

        [Fact]
        public void Initial_Item_DataContexts_Should_Be_Correct()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            for (var i = 0; i < target.Panel.Children.Count; ++i)
            {
                Assert.Equal(items[i], target.Panel.Children[i].DataContext);
            }
        }

        [Fact]
        public void Should_Add_New_Items_When_Control_Is_Enlarged()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);

            target.Measure(new Size(120, 120));
            target.Arrange(new Rect(0, 0, 100, 120));

            Assert.Equal(12, target.Panel.Children.Count);

            for (var i = 0; i < target.Panel.Children.Count; ++i)
            {
                Assert.Equal(items[i], target.Panel.Children[i].DataContext);
            }
        }

        [Fact]
        public void Should_Not_Create_Virtualizer_Before_Panel()
        {
            var target = CreateTarget();

            Assert.Null(target.Panel);
            Assert.Null(target.Virtualizer);
        }

        [Fact]
        public void Changing_VirtualizationMode_None_To_Simple_Should_Update_Control()
        {
            var target = CreateTarget(mode: ItemVirtualizationMode.None);
            var scroll = (ScrollContentPresenter)target.Parent;

            scroll.Measure(new Size(100, 100));
            scroll.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(20, target.Panel.Children.Count);
            Assert.Equal(new Size(100, 200), scroll.Extent);
            Assert.Equal(new Size(100, 100), scroll.Viewport);

            target.VirtualizationMode = ItemVirtualizationMode.Simple;
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);
            Assert.Equal(new Size(10, 20), scroll.Extent);
            Assert.Equal(new Size(100, 10), scroll.Viewport);
        }

        [Fact]
        public void Changing_VirtualizationMode_None_To_Simple_Should_Add_Correct_Number_Of_Controls()
        {
            using (UnitTestApplication.Start(new TestServices()))
            {
                var target = CreateTarget(mode: ItemVirtualizationMode.None);
                var scroll = (TestScroller)target.Parent;

                scroll.Width = scroll.Height = 100;
                scroll.LayoutManager.ExecuteInitialLayoutPass(scroll);

                // Ensure than an intermediate measure pass doesn't add more controls than it
                // should. This can happen if target gets measured with Size.Infinity which
                // is what the available size should be when VirtualizationMode == None but not
                // what it should after VirtualizationMode is changed to Simple.
                target.Panel.Children.CollectionChanged += (s, e) =>
                {
                    Assert.InRange(target.Panel.Children.Count, 0, 10);
                };

                target.VirtualizationMode = ItemVirtualizationMode.Simple;
                ((ILayoutRoot)scroll.GetVisualRoot()).LayoutManager.ExecuteLayoutPass();

                Assert.Equal(10, target.Panel.Children.Count);
            }
        }

        [Fact]
        public void Changing_VirtualizationMode_Simple_To_None_Should_Update_Control()
        {
            var target = CreateTarget();
            var scroll = (ScrollContentPresenter)target.Parent;

            scroll.Measure(new Size(100, 100));
            scroll.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);
            Assert.Equal(new Size(10, 20), scroll.Extent);
            Assert.Equal(new Size(100, 10), scroll.Viewport);

            target.VirtualizationMode = ItemVirtualizationMode.None;
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            // Here - unlike changing the other way - we need to do a layout pass on the scroll
            // content presenter as non-logical scroll values are only updated on arrange.
            scroll.Measure(new Size(100, 100));
            scroll.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(20, target.Panel.Children.Count);
            Assert.Equal(new Size(100, 200), scroll.Extent);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
        }

        private static ItemsPresenter CreateTarget(
            ItemVirtualizationMode mode = ItemVirtualizationMode.Simple,
            Orientation orientation = Orientation.Vertical,
            bool useContainers = true,
            int itemCount = 20)
        {
            ItemsPresenter result;
            var items = Enumerable.Range(0, itemCount).Select(x => $"Item {x}").ToList();

            var scroller = new TestScroller
            {
                CanHorizontallyScroll = false,
                CanVerticallyScroll = true,
                Content = result = new TestItemsPresenter(useContainers)
                {
                    Items = items,
                    ItemsPanel = VirtualizingPanelTemplate(orientation),
                    ItemTemplate = ItemTemplate(),
                    VirtualizationMode = mode,
                }
            };

            scroller.UpdateChild();

            return result;
        }

        private static IDataTemplate ItemTemplate()
        {
            return new FuncDataTemplate<string>((x, _) => new Canvas
            {
                Width = 10,
                Height = 10,
            });
        }

        private static ITemplate<IPanel> VirtualizingPanelTemplate(
            Orientation orientation = Orientation.Vertical)
        {
            return new FuncTemplate<IPanel>(() => new VirtualizingStackPanel
            {
                Orientation = orientation,
            });
        }

        private class TestScroller : ScrollContentPresenter, IRenderRoot, ILayoutRoot
        {
            public IRenderer Renderer { get; }
            public Size ClientSize { get; }
            public double RenderScaling => 1;

            public Size MaxClientSize => Size.Infinity;

            public double LayoutScaling => 1;

            public ILayoutManager LayoutManager { get; } = new LayoutManager();

            public IRenderTarget CreateRenderTarget() => throw new NotImplementedException();
            public void Invalidate(Rect rect) => throw new NotImplementedException();
            public Point PointToClient(PixelPoint p) => throw new NotImplementedException();
            public PixelPoint PointToScreen(Point p) => throw new NotImplementedException();
        }

        private class TestItemsPresenter : ItemsPresenter
        {
            private bool _useContainers;

            public TestItemsPresenter(bool useContainers)
            {
                _useContainers = useContainers;
            }

            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return _useContainers ?
                    new ItemContainerGenerator<TestContainer>(this, TestContainer.ContentProperty, null) :
                    new ItemContainerGenerator(this);
            }
        }

        private class TestContainer : ContentControl
        {
            public TestContainer()
            {
                Width = 10;
                Height = 10;
            }
        }
    }
}
