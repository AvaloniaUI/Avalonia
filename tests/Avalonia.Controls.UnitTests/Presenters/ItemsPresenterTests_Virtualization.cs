// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            var target = new ItemsPresenter
            {
            };

            target.ApplyTemplate();

            Assert.False(((IScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_False_When_VirtualizationMode_None()
        {
            var target = new ItemsPresenter
            {
                ItemsPanel = VirtualizingPanelTemplate(),
                VirtualizationMode = ItemVirtualizationMode.None,
            };

            target.ApplyTemplate();

            Assert.False(((IScrollable)target).IsLogicalScrollEnabled);
        }

        [Fact]
        public void Should_Return_IsLogicalScrollEnabled_True_When_Has_Virtualizing_Panel()
        {
            var target = new ItemsPresenter
            {
                ItemsPanel = VirtualizingPanelTemplate(),
            };

            target.ApplyTemplate();

            Assert.True(((IScrollable)target).IsLogicalScrollEnabled);
        }

        public class Simple
        {
            [Fact]
            public void Should_Return_Items_Count_For_Extent()
            {
                var target = new ItemsPresenter
                {
                    Items = new string[10],
                    ItemsPanel = VirtualizingPanelTemplate(),
                    VirtualizationMode = ItemVirtualizationMode.Simple,
                };

                target.ApplyTemplate();

                Assert.Equal(new Size(0, 10), ((IScrollable)target).Extent);
            }

            [Fact]
            public void Should_Have_Number_Of_Visible_Items_As_Viewport()
            {
                var target = new ItemsPresenter
                {
                    Items = new string[20],
                    ItemsPanel = VirtualizingPanelTemplate(),
                    ItemTemplate = ItemTemplate(),
                    VirtualizationMode = ItemVirtualizationMode.Simple,
                };

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                Assert.Equal(10, ((IScrollable)target).Viewport.Height);
            }
        }

        private static IDataTemplate ItemTemplate()
        {
            return new FuncDataTemplate<string>(x => new TextBlock
            {
                Text = x,
                Height = 10,
            });
        }

        private static ITemplate<IPanel> VirtualizingPanelTemplate()
        {
            return new FuncTemplate<IPanel>(() => new VirtualizingStackPanel());
        }
    }
}
