// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using static System.Math;

namespace Avalonia.Controls
{
    public class VirtualizingGridPanel : Panel, INavigableContainer, IVirtualizingPanel
    {
        private Size _availableSpace;
        private bool _forceRemeasure;
        private double _pixelOffset;
        private double _crossAxisOffset;

       
        /// <summary>
        /// Initializes static members of the <see cref="VirtualizingGridPanel"/> class.
        /// </summary>
        static VirtualizingGridPanel()
        {
            AffectsMeasure<VirtualizingGridPanel>(OrientationProperty,ItemWidthProperty,ItemHeightProperty);
        }
        
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<VirtualizingGridPanel, Orientation>(nameof(Orientation), defaultValue: Orientation.Horizontal);
      
        /// <summary>
        /// Defines the <see cref="ItemWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ItemWidthProperty =
            AvaloniaProperty.Register<VirtualizingGridPanel, int>(nameof(ItemWidth));

        /// <summary>
        /// Defines the <see cref="ItemHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ItemHeightProperty =
            AvaloniaProperty.Register<VirtualizingGridPanel, int>(nameof(ItemHeight));

        /// <summary>
        /// Gets or sets the orientation in which child controls will be layed out.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Specifies the width of child items.
        /// </summary>
        public int ItemWidth
        {
            get => GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        /// <summary>
        /// Specifies the height of child items.
        /// </summary>
        public int ItemHeight
        {
            get => GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }


        public IVirtualizingController Controller { get; set; }

        public bool IsFull => Children.Count >= MaxItems;

        public int OverflowCount => Max(Children.Count - MaxItems, 0);
       
        Orientation IVirtualizingPanel.ScrollDirection => Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;

        public double AverageItemSize => Orientation == Orientation.Horizontal ? ItemHeight : ItemWidth;

        double IVirtualizingPanel.PixelOverflow
        {
            get
            {
                var space = new UVSize(Orientation, _availableSpace);
                var itemSize = new UVSize(Orientation, ItemWidth, ItemHeight);

                (var rows, var cols) = RowsAndColums(_availableSpace);

                return Math.Max(0, rows * itemSize.V - space.V);
            }
        }
        double IVirtualizingPanel.PixelOffset
        {
            get { return _pixelOffset; }

            set
            {
                if (_pixelOffset != value)
                {
                    _pixelOffset = value;
                    InvalidateArrange();
                }
            }
        }

        double IVirtualizingPanel.CrossAxisOffset
        {
            get { return _crossAxisOffset; }

            set
            {
                if (_crossAxisOffset != value)
                {
                    _crossAxisOffset = value;
                    InvalidateArrange();
                }
            }
        }

        public int ScrollQuantum => Orientation == Orientation.Horizontal ? (int)Math.Max(1,_availableSpace.Width/ItemWidth) : (int)Math.Max(1, _availableSpace.Height / ItemHeight);

        public void ForceInvalidateMeasure()
        {
            InvalidateMeasure();
            _forceRemeasure = true;
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
            var result = GetControlInDirection(direction, from as IControl);

            if (result == null && wrap)
            {
                if (Orientation == Orientation.Vertical)
                {
                    switch (direction)
                    {
                        case NavigationDirection.Up:
                        case NavigationDirection.Previous:
                        case NavigationDirection.PageUp:
                            result = GetControlInDirection(NavigationDirection.Last, null);
                            break;
                        case NavigationDirection.Down:
                        case NavigationDirection.Next:
                        case NavigationDirection.PageDown:
                            result = GetControlInDirection(NavigationDirection.First, null);
                            break;
                    }
                }
                else
                {
                    switch (direction)
                    {
                        case NavigationDirection.Left:
                        case NavigationDirection.Previous:
                        case NavigationDirection.PageUp:
                            result = GetControlInDirection(NavigationDirection.Last, null);
                            break;
                        case NavigationDirection.Right:
                        case NavigationDirection.Next:
                        case NavigationDirection.PageDown:
                            result = GetControlInDirection(NavigationDirection.First, null);
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        protected virtual IInputElement GetControlInDirection(NavigationDirection direction, IControl from)
        {
            if (from == null)
                return null;

            var logicalScrollable = Parent as ILogicalScrollable;

            if (logicalScrollable?.IsLogicalScrollEnabled == true)
            {
                return logicalScrollable.GetControlInDirection(direction, from);
            }
            else
            {
                var horiz = Orientation == Orientation.Horizontal;
                int index = from != null ? Children.IndexOf(from) : -1;

                switch (direction)
                {
                    case NavigationDirection.First:
                        index = 0;
                        break;
                    case NavigationDirection.Last:
                        index = Children.Count - 1;
                        break;
                    case NavigationDirection.Next:
                        if (index != -1)
                            ++index;
                        break;
                    case NavigationDirection.Previous:
                        if (index != -1)
                            --index;
                        break;
                    case NavigationDirection.Left:
                        if (index != -1)
                            index = horiz ? index - 1 : -1;
                        break;
                    case NavigationDirection.Right:
                        if (index != -1)
                            index = horiz ? index + 1 : -1;
                        break;
                    case NavigationDirection.Up:
                        if (index != -1)
                            index = horiz ? -1 : index - 1;
                        break;
                    case NavigationDirection.Down:
                        if (index != -1)
                            index = horiz ? -1 : index + 1;
                        break;
                    default:
                        index = -1;
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
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            int rows;
            int cols;

            if (Orientation == Orientation.Horizontal)
            {
                if (availableSize.Width == double.PositiveInfinity)
                {
                    availableSize = availableSize.WithWidth(ItemWidth);
                }
                cols = Math.Max((int)(availableSize.Width / ItemWidth), 1);
                rows = (int)Math.Ceiling(((double)Children.Count / cols));

            }
            else
            {
                if (availableSize.Height == double.PositiveInfinity)
                {
                    availableSize = availableSize.WithHeight(ItemHeight);
                }
                rows = Math.Max((int)(availableSize.Height / ItemHeight), 1);
                cols = (int)Math.Ceiling(((double)Children.Count / rows));

            }

            if (_forceRemeasure || availableSize != ((ILayoutable)this).PreviousMeasure)
            {
                _forceRemeasure = false;
                _availableSpace = availableSize;
                Controller?.UpdateControls();
            }            
                    
            return Children.Count == 0 ? Size.Empty : new Size(cols * ItemWidth, rows * ItemHeight);
        }
        
        protected override Size ArrangeOverride(Size finalSize)
        {
            _availableSpace = finalSize;
            int rows;
            int cols;
            if (Orientation == Orientation.Horizontal)
            {
                cols = Max((int)(finalSize.Width / ItemWidth), 1);
                rows = (int)Ceiling(((double)Children.Count / cols));
                int x = 0;
                int y = 0;

                foreach (var child in Children)
                {
                    child.Arrange(new Rect(x * ItemWidth - _crossAxisOffset, y * ItemHeight - _pixelOffset, ItemWidth, ItemHeight));
                    x++;

                    if (x >= cols)
                    {
                        x = 0;
                        y++;
                    }
                }
            }
            else
            {
                rows = Max((int)(finalSize.Height / ItemHeight), 1);
                cols = (int)Ceiling(((double)Children.Count / rows));
                int x = 0;
                int y = 0;

                foreach (var child in Children)
                {
                    child.Arrange(new Rect(x * ItemWidth - _pixelOffset, y * ItemHeight - _crossAxisOffset, ItemWidth, ItemHeight));
                    y++;

                    if (y >= rows)
                    {
                        y = 0;
                        x++;
                    }
                }
             }
            Controller?.UpdateControls();
            finalSize = Children.Count == 0 ? Size.Empty : new Size(cols * ItemWidth , rows * ItemHeight);       
            return finalSize;
        }
        private int MaxItems
        {
            get
            {
                var space = new UVSize(Orientation, _availableSpace);
                var itemSize = new UVSize(Orientation, ItemWidth,ItemHeight);

                int uvCols = Max((int)(space.U / itemSize.U), 1);
                int uvRows = (int)Ceiling((space.V) / itemSize.V);

                return uvRows*uvCols;
            }
        }

        private (int uvRows, int uvColumns) RowsAndColums(Size size)
        {
            var uvSize = new UVSize(Orientation, size);
            var itemUVSize = new UVSize(Orientation, ItemWidth, ItemHeight);

            var ucols = Math.Max((int)(uvSize.U / itemUVSize.U), 1);
            var urows = (int)Math.Ceiling(((double)Children.Count / ucols));

            return (urows, ucols);
        }

        // taken from WrapPanel - should be turned into a general utility 

        /// <summary>
        /// Used to not not write separate code for horizontal and vertical orientation.
        /// U is direction in line. (x if orientation is horizontal)
        /// V is direction of lines. (y if orientation is horizontal)
        /// </summary>
        [DebuggerDisplay("U = {U} V = {V}")]
        private struct UVSize
        {
            private readonly Orientation _orientation;

            internal double U;

            internal double V;

            internal UVSize(Orientation orientation, double width, double height)
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
