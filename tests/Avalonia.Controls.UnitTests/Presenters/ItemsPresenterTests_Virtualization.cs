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
    public class ItemsPresenterTests_Virtualization
    {
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
        public void Should_Fill_Panel_With_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
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

            target.Arrange(new Rect(0, 0, 100, 120));

            Assert.Equal(12, target.Panel.Children.Count);

            for (var i = 0; i < target.Panel.Children.Count; ++i)
            {
                Assert.Equal(items[i], target.Panel.Children[i].DataContext);
            }
        }

        public class Simple
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
        }

        private static ItemsPresenter CreateTarget(
            ItemVirtualizationMode mode = ItemVirtualizationMode.Simple,
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
                    VirtualizationMode = mode,
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
