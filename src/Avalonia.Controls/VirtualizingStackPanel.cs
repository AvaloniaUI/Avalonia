// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives;
using System;
using System.Collections.Specialized;

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : StackPanel, IScrollable, IVirtualizingPanel
    {
        private double _takenSpace;
        private int _canBeRemoved;

        bool IVirtualizingPanel.IsFull
        {
            get
            {
                return Orientation == Orientation.Horizontal ?
                    _takenSpace >= Bounds.Width :
                    _takenSpace >= Bounds.Height;
            }
        }

        int IVirtualizingPanel.OverflowCount => _canBeRemoved;

        Action IVirtualizingPanel.ArrangeCompleted { get; set; }

        Action IScrollable.InvalidateScroll
        {
            get;
            set;
        }

        Size IScrollable.Extent => new Size(_takenSpace, _takenSpace);

        Vector IScrollable.Offset
        {
            get { return default(Vector); }
            set { }
        }

        Size IScrollable.Viewport => Bounds.Size;

        Size IScrollable.ScrollSize => new Size(1, 1);

        Size IScrollable.PageScrollSize => new Size(1, 1);

        protected override Size ArrangeOverride(Size finalSize)
        {
            _canBeRemoved = 0;
            _takenSpace = 0;
            var result = base.ArrangeOverride(finalSize);
            ((IVirtualizingPanel)this).ArrangeCompleted?.Invoke();
            return result;
        }

        protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ChildrenChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IControl control in e.NewItems)
                    {
                        UpdatePhysicalSizeForAdd(control);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (IControl control in e.OldItems)
                    {
                        UpdatePhysicalSizeForRemove(control);
                    }

                    break;
            }
        }

        internal override void ArrangeChild(
            IControl child, 
            Rect rect,
            Size panelSize,
            Orientation orientation)
        {
            base.ArrangeChild(child, rect, panelSize, orientation);

            if (orientation == Orientation.Horizontal)
            {
                if (rect.X >= panelSize.Width)
                {
                    ++_canBeRemoved;
                }

                if (rect.Right >= _takenSpace)
                {
                    _takenSpace = rect.Right;
                }
            }
            else
            {
                if (rect.Y >= panelSize.Height)
                {
                    ++_canBeRemoved;
                }

                if (rect.Bottom >= _takenSpace)
                {
                    _takenSpace = rect.Bottom;
                }
            }
        }

        private void UpdatePhysicalSizeForAdd(IControl child)
        {
            var bounds = Bounds;
            var gap = Gap;

            child.Measure(bounds.Size);

            if (Orientation == Orientation.Vertical)
            {
                _takenSpace += child.DesiredSize.Height + gap;
            }
            else
            {
                _takenSpace += child.DesiredSize.Width + gap;
            }
        }

        private void UpdatePhysicalSizeForRemove(IControl child)
        {
            var bounds = Bounds;
            var gap = Gap;

            if (Orientation == Orientation.Vertical)
            {
                _takenSpace -= child.DesiredSize.Height + gap;
            }
            else
            {
                _takenSpace -= child.DesiredSize.Width + gap;
            }
        }
    }
}
