// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Moq;
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

            Assert.False(((IScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_False_When_VirtualizationMode_None()
        {
            var target = CreateTarget(ItemVirtualizationMode.None);

            target.ApplyTemplate();

            Assert.False(((IScrollable)target).IsLogicalScrollEnabled);
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

            Assert.False(((IScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_True()
        {
            var target = CreateTarget();

            target.ApplyTemplate();

            Assert.True(((IScrollable)target).IsLogicalScrollEnabled);
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
            public void Should_Return_Items_Count_For_Extent()
            {
                var target = CreateTarget();

                target.ApplyTemplate();

                Assert.Equal(new Size(0, 20), ((IScrollable)target).Extent);
            }

            [Fact]
            public void Should_Have_Number_Of_Visible_Items_As_Viewport()
            {
                var target = CreateTarget();

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                Assert.Equal(10, ((IScrollable)target).Viewport.Height);
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
        }

        private static ItemsPresenter CreateTarget(
            ItemVirtualizationMode mode = ItemVirtualizationMode.Simple,
            int itemCount = 20)
        {
            ItemsPresenter result;
            var items = Enumerable.Range(0, itemCount).Select(x => $"Item {x}").ToList();

            var scroller = new ScrollContentPresenter
            {
                Content = result = new ItemsPresenter
                {
                    Items = items,
                    ItemsPanel = VirtualizingPanelTemplate(),
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
                Height = 10,
            });
        }

        private static ITemplate<IPanel> VirtualizingPanelTemplate()
        {
            return new FuncTemplate<IPanel>(() => new VirtualizingStackPanel());
        }
    }
}
