// Ported from https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite/Panels/RelativePanel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an area within which you can position and align child objects in relation to each other or the parent panel.
    /// </summary>
    public partial class RelativePanel : Panel
    {
        private readonly Dictionary<Control, Rect> _childLayouts = new();
        private readonly HashSet<Control> _layoutQueue = new();

        private Rect MeasureChild(Control uiElement, Size availableSize)
        {
            if (_layoutQueue.Contains(uiElement))
            {
                throw new InvalidOperationException("Circular dependency detected");
            }

            _layoutQueue.Add(uiElement);

            uiElement.Measure(availableSize);

            Rect layoutInfo = new(double.NaN, double.NaN, uiElement.DesiredSize.Width, uiElement.DesiredSize.Height);

            #region Horizontal Position

            if (GetAlignLeftWithPanel(uiElement))
            {
                layoutInfo = layoutInfo.WithX( 0);
            }

            if (GetAlignRightWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithX( 0);
            }

            if (GetAlignLeftWith(uiElement) is Control alignLeftWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignLeftWith, out var alignLeftWithLayout))
                {
                    _childLayouts[alignLeftWith] = alignLeftWithLayout = MeasureChild(alignLeftWith, availableSize);
                }

                layoutInfo = layoutInfo.WithX(alignLeftWithLayout.Left);
            }

            if (GetAlignRightWith(uiElement) is Control alignRightWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignRightWith, out var alignRightWithLayout))
                {
                    _childLayouts[alignRightWith] = alignRightWithLayout = MeasureChild(alignRightWith, availableSize);
                }

                layoutInfo = layoutInfo.WithX(alignRightWithLayout.Left + alignRightWithLayout.Size.Width - layoutInfo.Size.Width);
            }

            if (GetRightOf(uiElement) is Control rightOf)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(rightOf, out var rightOfLayout))
                {
                    _childLayouts[rightOf] = rightOfLayout = MeasureChild(rightOf, availableSize);
                }

                layoutInfo = layoutInfo.WithX(rightOfLayout.Left + rightOfLayout.Size.Width);
            }

            if (GetLeftOf(uiElement) is Control leftOf)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(leftOf, out var leftOfLayout))
                {
                    _childLayouts[leftOf] = leftOfLayout = MeasureChild(leftOf, availableSize);
                }

                layoutInfo = layoutInfo.WithX(leftOfLayout.Left - layoutInfo.Size.Width);
            }

            if (GetAlignHorizontalCenterWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithX(0);
            }

            if (GetAlignHorizontalCenterWith(uiElement) is Control alignHorizontalCenterWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignHorizontalCenterWith, out var alignHorizontalCenterWithLayout))
                {
                    _childLayouts[alignHorizontalCenterWith] = alignHorizontalCenterWithLayout = MeasureChild(alignHorizontalCenterWith, availableSize);
                }

                layoutInfo = layoutInfo.WithX(alignHorizontalCenterWithLayout.Left - (alignHorizontalCenterWithLayout.Size.Width - layoutInfo.Size.Width) / 2);
            }

            if (double.IsNaN(layoutInfo.X))
            {
                layoutInfo = layoutInfo.WithX(0);
            }

            #endregion

            #region Vertical position

            if (GetAlignTopWithPanel(uiElement))
            {
                layoutInfo = layoutInfo.WithY(0);
            }

            if (GetAlignRightWithPanel(uiElement))
            {
                layoutInfo = layoutInfo.WithY(0);
            }

            if (GetAlignTopWith(uiElement) is Control alignTopWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignTopWith, out var alignTopWithLayout))
                {
                    _childLayouts[alignTopWith] = alignTopWithLayout = MeasureChild(alignTopWith, availableSize);
                }

                layoutInfo = layoutInfo.WithY(alignTopWithLayout.Top);
            }

            if (GetAlignBottomWith(uiElement) is Control alignBottomWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignBottomWith, out var alignBottomWithLayout))
                {
                    _childLayouts[alignBottomWith] = alignBottomWithLayout = MeasureChild(alignBottomWith, availableSize);
                }

                layoutInfo = layoutInfo.WithY(alignBottomWithLayout.Top + alignBottomWithLayout.Size.Height - layoutInfo.Size.Height);
            }

            if (GetBelow(uiElement) is Control below)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(below, out var belowLayout))
                {
                    _childLayouts[below] = belowLayout = MeasureChild(below, availableSize);
                }

                layoutInfo = layoutInfo.WithY(belowLayout.Top + belowLayout.Size.Height);
            }

            if (GetAbove(uiElement) is Control above)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(above, out var aboveLayout))
                {
                    _childLayouts[above] = aboveLayout = MeasureChild(above, availableSize);
                }

                layoutInfo = layoutInfo.WithY(aboveLayout.Top - layoutInfo.Size.Height);
            }

            if (GetAlignVerticalCenterWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithY(0);
            }

            if (GetAlignVerticalCenterWith(uiElement) is Control alignVerticalCenterWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignVerticalCenterWith, out var alignVerticalCenterWithLayout))
                {
                    _childLayouts[alignVerticalCenterWith] = alignVerticalCenterWithLayout = MeasureChild(alignVerticalCenterWith, availableSize);
                }

                layoutInfo = layoutInfo.WithY(alignVerticalCenterWithLayout.Top - (alignVerticalCenterWithLayout.Size.Height - layoutInfo.Size.Height) / 2);
            }

            if (double.IsNaN(layoutInfo.Y))
            {
                layoutInfo = layoutInfo.WithY(0);
            }

            #endregion

            _layoutQueue.Remove(uiElement);

            return layoutInfo;
        }

        private Rect ArrangeChild(Control uiElement, Size arrangeSize)
        {
            if (_layoutQueue.Contains(uiElement))
            {
                throw new InvalidOperationException("Circular dependency detected");
            }

            _layoutQueue.Add(uiElement);

            if (arrangeSize.Width < uiElement.DesiredSize.Width ||
                arrangeSize.Height < uiElement.DesiredSize.Height)
            {
                uiElement.Measure(arrangeSize);
            }

            Rect layoutInfo = new(double.NaN, double.NaN, uiElement.DesiredSize.Width, uiElement.DesiredSize.Height);

            #region Horizontal Position

            if (GetAlignLeftWithPanel(uiElement))
            {
                layoutInfo = layoutInfo.WithX(0);
            }

            if (GetAlignRightWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithX(arrangeSize.Width - layoutInfo.Size.Width);
            }

            if (GetAlignLeftWith(uiElement) is Control alignLeftWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignLeftWith, out var alignLeftWithLayout))
                {
                    _childLayouts[alignLeftWith] = alignLeftWithLayout = ArrangeChild(alignLeftWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithX(alignLeftWithLayout.Left);
            }

            if (GetAlignRightWith(uiElement) is Control alignRightWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignRightWith, out var alignRightWithLayout))
                {
                    _childLayouts[alignRightWith] = alignRightWithLayout = ArrangeChild(alignRightWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithX(alignRightWithLayout.Left + alignRightWithLayout.Size.Width - layoutInfo.Size.Width);
            }

            if (GetRightOf(uiElement) is Control rightOf)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(rightOf, out var rightOfLayout))
                {
                    _childLayouts[rightOf] = rightOfLayout = ArrangeChild(rightOf, arrangeSize);
                }

                layoutInfo = layoutInfo.WithX(rightOfLayout.Left + rightOfLayout.Size.Width);
            }

            if (GetLeftOf(uiElement) is Control leftOf)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(leftOf, out var leftOfLayout))
                {
                    _childLayouts[leftOf] = leftOfLayout = ArrangeChild(leftOf, arrangeSize);
                }

                layoutInfo = layoutInfo.WithX(leftOfLayout.Left - layoutInfo.Size.Width);
            }

            if (GetAlignHorizontalCenterWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithX((arrangeSize.Width - layoutInfo.Size.Width) / 2);
            }

            if (GetAlignHorizontalCenterWith(uiElement) is Control alignHorizontalCenterWith)
            {
                if (!double.IsNaN(layoutInfo.Left))
                {
                    throw new InvalidOperationException("Horizontal position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignHorizontalCenterWith, out var alignHorizontalCenterWithLayout))
                {
                    _childLayouts[alignHorizontalCenterWith] = alignHorizontalCenterWithLayout = ArrangeChild(alignHorizontalCenterWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithX(alignHorizontalCenterWithLayout.Left - (alignHorizontalCenterWithLayout.Size.Width - layoutInfo.Size.Width) / 2);
            }

            if (double.IsNaN(layoutInfo.X))
            {
                layoutInfo = layoutInfo.WithX(0);
            }

            #endregion

            #region Vertical position

            if (GetAlignTopWithPanel(uiElement))
            {
                layoutInfo = layoutInfo.WithY(0);
            }

            if (GetAlignBottomWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithY(0);
            }

            if (GetAlignTopWith(uiElement) is Control alignTopWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignTopWith, out var alignTopWithLayout))
                {
                    _childLayouts[alignTopWith] = alignTopWithLayout = ArrangeChild(alignTopWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithY(alignTopWithLayout.Top);
            }

            if (GetAlignBottomWith(uiElement) is Control alignBottomWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignBottomWith, out var alignBottomWithLayout))
                {
                    _childLayouts[alignBottomWith] = alignBottomWithLayout = ArrangeChild(alignBottomWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithY(alignBottomWithLayout.Top + alignBottomWithLayout.Size.Height - layoutInfo.Size.Height);
            }

            if (GetBelow(uiElement) is Control below)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(below, out var belowLayout))
                {
                    _childLayouts[below] = belowLayout = ArrangeChild(below, arrangeSize);
                }

                layoutInfo = layoutInfo.WithY(belowLayout.Top + belowLayout.Size.Height);
            }

            if (GetAbove(uiElement) is Control above)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(above, out var aboveLayout))
                {
                    _childLayouts[above] = aboveLayout = ArrangeChild(above, arrangeSize);
                }

                layoutInfo = layoutInfo.WithY(aboveLayout.Top - layoutInfo.Size.Height);
            }

            if (GetAlignVerticalCenterWithPanel(uiElement))
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                layoutInfo = layoutInfo.WithY((arrangeSize.Height - layoutInfo.Size.Height) / 2);
            }

            if (GetAlignVerticalCenterWith(uiElement) is Control alignVerticalCenterWith)
            {
                if (!double.IsNaN(layoutInfo.Top))
                {
                    throw new InvalidOperationException("Vertical position of Control can be set only once");
                }

                if (!_childLayouts.TryGetValue(alignVerticalCenterWith, out var alignVerticalCenterWithLayout))
                {
                    _childLayouts[alignVerticalCenterWith] = alignVerticalCenterWithLayout = ArrangeChild(alignVerticalCenterWith, arrangeSize);
                }

                layoutInfo = layoutInfo.WithY(alignVerticalCenterWithLayout.Top - (alignVerticalCenterWithLayout.Size.Height - layoutInfo.Size.Height) / 2);
            }

            if (double.IsNaN(layoutInfo.Y))
            {
                layoutInfo = layoutInfo.WithY(0);
            }

            #endregion

            _layoutQueue.Remove(uiElement);

            uiElement.Arrange(new Rect(layoutInfo.Left, layoutInfo.Top, layoutInfo.Size.Width, layoutInfo.Size.Height));

            return layoutInfo;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (Control child in Children)
            {
                if (!_childLayouts.ContainsKey(child))
                {
                    _childLayouts[child] = MeasureChild(child, availableSize);
                }
            }

            double left = 0;
            double top = 0;
            double right = 0;
            double bottom = 0;

            foreach (var layout in _childLayouts.Values)
            {
                left = Math.Min(left, layout.Left);
                top = Math.Min(top, layout.Top);
                right = Math.Max(right, layout.Left + layout.Size.Width);
                bottom = Math.Max(bottom, layout.Top + layout.Size.Height);
            }

            var size = new Size(
                Math.Min(right - left, availableSize.Width), 
                Math.Min(bottom - top, availableSize.Height));

            _childLayouts.Clear();

            return size;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (Control child in Children)
            {
                if (!_childLayouts.ContainsKey(child))
                {
                    _childLayouts[child] = ArrangeChild(child, arrangeSize);
                }
            }

            _childLayouts.Clear();

            return arrangeSize;
        }
    }
}
