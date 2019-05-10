// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements a <see cref="StackPanel"/> where the flow direction of its items can be reversed.
    /// </summary>
    public class ReversibleStackPanel : StackPanel
    {
        /// <summary>
        /// Defines the <see cref="ReverseOrder"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ReverseOrderProperty =
            AvaloniaProperty.Register<ReversibleStackPanel, bool>(nameof(ReverseOrder));

        /// <summary>
        /// Gets or sets if the child controls will be layed out in reverse order.
        /// </summary>
        public bool ReverseOrder
        {
            get => GetValue(ReverseOrderProperty);
            set => SetValue(ReverseOrderProperty, value);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var orientation = Orientation;
            var spacing = Spacing;
            var finalRect = new Rect(finalSize);
            var pos = 0.0;

            var children = ReverseOrder ? Children.Reverse() : Children;

            foreach (Control child in children)
            {
                double childWidth = child.DesiredSize.Width;
                double childHeight = child.DesiredSize.Height;

                if (orientation == Orientation.Vertical)
                {
                    var rect = new Rect(0, pos, childWidth, childHeight)
                        .Align(finalRect, child.HorizontalAlignment, VerticalAlignment.Top);
                    ArrangeChild(child, rect, finalSize, orientation);
                    pos += childHeight + spacing;
                }
                else
                {
                    var rect = new Rect(pos, 0, childWidth, childHeight)
                        .Align(finalRect, HorizontalAlignment.Left, child.VerticalAlignment);
                    ArrangeChild(child, rect, finalSize, orientation);
                    pos += childWidth + spacing;
                }
            }

            return finalSize;
        }
    }
}
