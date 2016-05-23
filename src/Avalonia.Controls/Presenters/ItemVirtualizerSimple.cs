// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Presenters
{
    internal class ItemVirtualizerSimple : ItemVirtualizer
    {
        public ItemVirtualizerSimple(ItemsPresenter owner)
            : base(owner)
        {
        }

        public override bool IsLogicalScrollEnabled => true;

        public override Size Extent
        {
            get
            {
                if (VirtualizingPanel.ScrollDirection == Orientation.Vertical)
                {
                    return new Size(0, Items.Count());
                }
                else
                {
                    return new Size(Items.Count(), 0);
                }
            }
        }

        public override Size Viewport
        {
            get
            {
                var panel = VirtualizingPanel;

                if (panel.ScrollDirection == Orientation.Vertical)
                {
                    return new Size(0, panel.Children.Count);
                }
                else
                {
                    return new Size(panel.Children.Count, 0);
                }
            }
        }

        public override void Arranging(Size finalSize)
        {
            CreateRemoveContainers();
        }

        private void CreateRemoveContainers()
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;

            if (!panel.IsFull)
            {
                var index = LastIndex + 1;
                var items = Items.Cast<object>().Skip(index);
                var memberSelector = Owner.MemberSelector;

                foreach (var item in items)
                {
                    var materialized = generator.Materialize(index++, item, memberSelector);
                    panel.Children.Add(materialized.ContainerControl);

                    if (panel.IsFull)
                    {
                        break;
                    }
                }

                LastIndex = index - 1;
            }

            if (panel.OverflowCount > 0)
            {
                var count = panel.OverflowCount;
                var index = panel.Children.Count - count;

                panel.Children.RemoveRange(index, count);
                generator.Dematerialize(index, count);

                LastIndex -= count;
            }
        }
    }
}
