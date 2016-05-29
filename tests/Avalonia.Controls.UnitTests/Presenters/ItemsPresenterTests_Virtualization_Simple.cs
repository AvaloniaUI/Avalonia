// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ItemsPresenterTests_Virtualization_Simple
    {
        [Fact]
        public void Should_Return_Items_Count_For_Extent_Vertical()
        {
            var target = CreateTarget();

            target.ApplyTemplate();

            Assert.Equal(new Size(0, 20), ((ILogicalScrollable)target).Extent);
        }

        [Fact]
        public void Should_Return_Items_Count_For_Extent_Horizontal()
        {
            var target = CreateTarget(orientation: Orientation.Horizontal);

            target.ApplyTemplate();

            Assert.Equal(new Size(20, 0), ((ILogicalScrollable)target).Extent);
        }

        [Fact]
        public void Should_Have_Number_Of_Visible_Items_As_Viewport_Vertical()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(0, 10), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Should_Have_Number_Of_Visible_Items_As_Viewport_Horizontal()
        {
            var target = CreateTarget(orientation: Orientation.Horizontal);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(10, 0), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Should_Remove_Items_When_Control_Is_Shrank()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);

            target.Arrange(new Rect(0, 0, 100, 80));

            Assert.Equal(8, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Add_New_Items_At_Top_When_Control_Is_Scrolled_To_Bottom_And_Enlarged()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);

            ((IScrollable)target).Offset = new Vector(0, 10);
            target.Arrange(new Rect(0, 0, 100, 120));

            Assert.Equal(12, target.Panel.Children.Count);

            for (var i = 0; i < target.Panel.Children.Count; ++i)
            {
                Assert.Equal(items[i + 8], target.Panel.Children[i].DataContext);
            }
        }

        [Fact]
        public void Should_Update_Containers_When_Items_Changes()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            target.Items = new[] { "foo", "bar", "baz" };

            Assert.Equal(3, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Decrease_The_Viewport_Size_By_One_If_There_Is_A_Partial_Item()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 95));
            target.Arrange(new Rect(0, 0, 100, 95));

            Assert.Equal(new Size(0, 9), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Moving_To_And_From_The_End_With_Partial_Item_Should_Set_Panel_PixelOffset()
        {
            var target = CreateTarget(itemCount: 20);

            target.ApplyTemplate();
            target.Measure(new Size(100, 95));
            target.Arrange(new Rect(0, 0, 100, 95));

            ((ILogicalScrollable)target).Offset = new Vector(0, 11);

            var minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 11), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(5, ((IVirtualizingPanel)target.Panel).PixelOffset);

            ((ILogicalScrollable)target).Offset = new Vector(0, 10);

            minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 10), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(0, ((IVirtualizingPanel)target.Panel).PixelOffset);

            ((ILogicalScrollable)target).Offset = new Vector(0, 11);

            minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 11), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(5, ((IVirtualizingPanel)target.Panel).PixelOffset);
        }

        public class WithContainers
        {
            [Fact]
            public void Scrolling_Less_Than_A_Page_Should_Move_Recycled_Items()
            {
                var target = CreateTarget();
                var items = (IList<string>)target.Items;

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var containers = target.Panel.Children.ToList();
                var scroller = (ScrollContentPresenter)target.Parent;

                scroller.Offset = new Vector(0, 5);

                var scrolledContainers = containers
                    .Skip(5)
                    .Take(5)
                    .Concat(containers.Take(5)).ToList();

                Assert.Equal(new Vector(0, 5), ((ILogicalScrollable)target).Offset);
                Assert.Equal(scrolledContainers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i + 5], target.Panel.Children[i].DataContext);
                }

                scroller.Offset = new Vector(0, 0);
                Assert.Equal(new Vector(0, 0), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                var dcs = target.Panel.Children.Select(x => x.DataContext).ToList();

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i], target.Panel.Children[i].DataContext);
                }
            }

            [Fact]
            public void Scrolling_More_Than_A_Page_Should_Recycle_Items()
            {
                var target = CreateTarget(itemCount: 50);
                var items = (IList<string>)target.Items;

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var containers = target.Panel.Children.ToList();
                var scroller = (ScrollContentPresenter)target.Parent;

                scroller.Offset = new Vector(0, 20);

                Assert.Equal(new Vector(0, 20), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i + 20], target.Panel.Children[i].DataContext);
                }

                scroller.Offset = new Vector(0, 0);

                Assert.Equal(new Vector(0, 0), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i], target.Panel.Children[i].DataContext);
                }
            }
        }

        private static ItemsPresenter CreateTarget(
            Orientation orientation = Orientation.Vertical,
            bool useContainers = true,
            int itemCount = 20)
        {
            ItemsPresenter result;
            var items = Enumerable.Range(0, itemCount).Select(x => $"Item {x}").ToList();

            var scroller = new ScrollContentPresenter
            {
                Content = result = new TestItemsPresenter(useContainers)
                {
                    Items = items,
                    ItemsPanel = VirtualizingPanelTemplate(orientation),
                    ItemTemplate = ItemTemplate(),
                    VirtualizationMode = ItemVirtualizationMode.Simple,
                }
            };

            scroller.UpdateChild();

            return result;
        }

        private static IDataTemplate ItemTemplate()
        {
            return new FuncDataTemplate<string>(x => new Canvas
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
