// -----------------------------------------------------------------------
// <copyright file="LayoutHelper.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Controls;

    public static class LayoutHelper
    {
        public static Size MeasureDecorator(
            Control decorator,
            Control content,
            Size availableSize, 
            Thickness padding)
        {
            double width = 0;
            double height = 0;

            if (decorator.Visibility != Visibility.Collapsed)
            {
                if (content != null)
                {
                    content.Measure(availableSize);
                    Size s = content.DesiredSize.Value.Inflate(padding);
                    width = s.Width;
                    height = s.Height;
                }

                if (decorator.Width > 0)
                {
                    width = decorator.Width;
                }

                if (decorator.Height > 0)
                {
                    height = decorator.Height;
                }
            }

            return new Size(width, height);
        }

        public static Size ArrangeDecorator(
            Control decorator,
            Control content,
            Size finalSize,
            Thickness padding)
        {
            if (content != null)
            {
                Rect childRect = AlignChild(
                    new Rect(finalSize).Deflate(padding),
                    content.DesiredSize.Value,
                    content.HorizontalAlignment,
                    content.VerticalAlignment);

                content.Arrange(childRect);
            }

            return finalSize;
        }

        public static Rect AlignChild(
            Rect parentRect,
            Size desiredSize,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment)
        {
            double x;
            double y;
            double width;
            double height;

            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Stretch:
                    width = parentRect.Width;
                    x = parentRect.X;
                    break;

                case HorizontalAlignment.Left:
                    width = desiredSize.Width;
                    x = parentRect.X;
                    break;

                case HorizontalAlignment.Center:
                    width = desiredSize.Width;
                    x = (parentRect.Width - width) / 2;
                    break;

                case HorizontalAlignment.Right:
                    width = desiredSize.Width;
                    x = parentRect.Right - width;
                    break;

                default:
                    throw new InvalidOperationException("Invalid HorizontalAlignment.");
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Stretch:
                    height = parentRect.Height;
                    y = parentRect.Y;
                    break;

                case VerticalAlignment.Top:
                    height = desiredSize.Height;
                    y = parentRect.Y;
                    break;

                case VerticalAlignment.Center:
                    height = desiredSize.Height;
                    y = (parentRect.Height - height) / 2;
                    break;

                case VerticalAlignment.Bottom:
                    height = desiredSize.Height;
                    y = parentRect.Bottom - height;
                    break;

                default:
                    throw new InvalidOperationException("Invalid VerticalAlignment.");
            }

            return new Rect(x, y, width, height);
        }
    }
}
