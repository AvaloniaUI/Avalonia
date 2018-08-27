// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Avalonia.Input;

using static System.Math;

namespace Avalonia.Controls
{
    /// <summary>
    /// Positions child elements in sequential position from left to right, 
    /// breaking content to the next line at the edge of the containing box. 
    /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
    /// depending on the value of the Orientation property.
    /// </summary>
    public class WrapPanel : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<WrapPanel, Orientation>(nameof(Orientation), defaultValue: Orientation.Horizontal);

        /// <summary>
        /// Initializes static members of the <see cref="WrapPanel"/> class.
        /// </summary>
        static WrapPanel()
        {
            AffectsMeasure(OrientationProperty);
        }

        /// <summary>
        /// Gets or sets the orientation in which child controls will be layed out.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement INavigableContainer.GetControl(NavigationDirection direction, IInputElement from, bool wrap)
        {
            var horiz = Orientation == Orientation.Horizontal;
            int index = Children.IndexOf((IControl)from);

            switch (direction)
            {
                case NavigationDirection.First:
                    index = 0;
                    break;
                case NavigationDirection.Last:
                    index = Children.Count - 1;
                    break;
                case NavigationDirection.Next:
                    ++index;
                    break;
                case NavigationDirection.Previous:
                    --index;
                    break;
                case NavigationDirection.Left:
                    index = horiz ? index - 1 : -1;
                    break;
                case NavigationDirection.Right:
                    index = horiz ? index + 1 : -1;
                    break;
                case NavigationDirection.Up:
                    index = horiz ? -1 : index - 1;
                    break;
                case NavigationDirection.Down:
                    index = horiz ? -1 : index + 1;
                    break;
            }

            if (index >= 0 && index < Children.Count)
            {
                return Children[index];
            }
            else
            {
                return null;
            }
        }

        private UVSize CreateUVSize(Size size) => new UVSize(Orientation, size);

        private UVSize CreateUVSize() => new UVSize(Orientation);

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var desiredSize = CreateUVSize();
            var lineSize = CreateUVSize();
            var uvAvailableSize = CreateUVSize(availableSize);

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                var childSize = CreateUVSize(child.DesiredSize);
                if (lineSize.U + childSize.U <= uvAvailableSize.U) // same line
                {
                    lineSize.U += childSize.U;
                    lineSize.V = Max(lineSize.V, childSize.V);
                }
                else // moving to next line
                {
                    desiredSize.U = Max(lineSize.U, uvAvailableSize.U);
                    desiredSize.V += lineSize.V;
                    lineSize = childSize;
                }
            }
            // last element
            desiredSize.U = Max(lineSize.U, desiredSize.U);
            desiredSize.V += lineSize.V;

            return desiredSize.ToSize();
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double accumulatedV = 0;
            var uvFinalSize = CreateUVSize(finalSize);
            var lineSize = CreateUVSize();
            int firstChildInLineindex = 0;
            for (int index = 0; index < Children.Count; index++)
            {
                var child = Children[index];
                var childSize = CreateUVSize(child.DesiredSize);
                if (lineSize.U + childSize.U <= uvFinalSize.U) // same line
                {
                    lineSize.U += childSize.U;
                    lineSize.V = Max(lineSize.V, childSize.V);
                }
                else // moving to next line
                {
                    var controlsInLine = GetContolsBetween(firstChildInLineindex, index);
                    ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
                    accumulatedV += lineSize.V;
                    lineSize = childSize;
                    firstChildInLineindex = index;
                }
            }

            if (firstChildInLineindex < Children.Count)
            {
                var controlsInLine = GetContolsBetween(firstChildInLineindex, Children.Count);
                ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
            }
            return finalSize;
        }

        private IEnumerable<IControl> GetContolsBetween(int first, int last)
        {
            return Children.Skip(first).Take(last - first);
        }

        private void ArrangeLine(double v, double lineV, IEnumerable<IControl> contols)
        {
            double u = 0;
            bool isHorizontal = (Orientation == Orientation.Horizontal);
            foreach (var child in contols)
            {
                var childSize = CreateUVSize(child.DesiredSize);
                var x = isHorizontal ? u : v;
                var y = isHorizontal ? v : u;
                var width = isHorizontal ? childSize.U : lineV;
                var height = isHorizontal ? lineV : childSize.U;
                child.Arrange(new Rect(x, y, width, height));
                u += childSize.U;
            }
        }
        /// <summary>
        /// Used to not not write sepearate code for horizontal and vertical orientation.
        /// U is direction in line. (x if orientation is horizontal)
        /// V is direction of lines. (y if orientation is horizonral)
        /// </summary>
        [DebuggerDisplay("U = {U} V = {V}")]
        private struct UVSize
        {
            private readonly Orientation _orientation;

            internal double U;

            internal double V;

            private UVSize(Orientation orientation, double width, double height)
            {
                U = V = 0d;
                _orientation = orientation;
                Width = width;
                Height = height;
            }

            internal UVSize(Orientation orientation, Size size)
                : this(orientation, size.Width, size.Height)
            {
            }

            internal UVSize(Orientation orientation)
            {
                U = V = 0d;
                _orientation = orientation;
            }

            private double Width
            {
                get { return (_orientation == Orientation.Horizontal ? U : V); }
                set
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        U = value;
                    }
                    else
                    {
                        V = value;
                    }
                }
            }

            private double Height
            {
                get { return (_orientation == Orientation.Horizontal ? V : U); }
                set
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        V = value;
                    }
                    else
                    {
                        U = value;
                    }
                }
            }

            public Size ToSize()
            {
                return new Size(Width, Height);
            }
        }
    }
}
