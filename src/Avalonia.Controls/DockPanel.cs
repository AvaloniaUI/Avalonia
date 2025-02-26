// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the available docking modes for a control in a <see cref="DockPanel"/>.
    /// </summary>
    public enum Dock
    {
        Left = 0,
        Bottom,
        Right,
        Top
    }

    /// <summary>
    /// A panel which arranges its children at the top, bottom, left, right or center.
    /// </summary>
    public class DockPanel : Panel
    {
        /// <summary>
        /// Defines the Dock attached property.
        /// </summary>
        public static readonly AttachedProperty<Dock> DockProperty =
            AvaloniaProperty.RegisterAttached<DockPanel, Control, Dock>("Dock");

        /// <summary>
        /// Defines the <see cref="LastChildFill"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> LastChildFillProperty =
            AvaloniaProperty.Register<DockPanel, bool>(
                nameof(LastChildFill),
                defaultValue: true);

        /// <summary>
        /// Identifies the HorizontalSpacing dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="HorizontalSpacing"/> dependency property.</returns>
        public static readonly StyledProperty<double> HorizontalSpacingProperty =
            AvaloniaProperty.Register<DockPanel, double>(
                nameof(HorizontalSpacing));

        /// <summary>
        /// Identifies the VerticalSpacing dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="VerticalSpacing"/> dependency property.</returns>
        public static readonly StyledProperty<double> VerticalSpacingProperty =
                AvaloniaProperty.Register<DockPanel, double>(
                    nameof(VerticalSpacing));

        /// <summary>
        /// Initializes static members of the <see cref="DockPanel"/> class.
        /// </summary>
        static DockPanel()
        {
            AffectsParentMeasure<DockPanel>(DockProperty);
            AffectsMeasure<DockPanel>(LastChildFillProperty, HorizontalSpacingProperty, VerticalSpacingProperty);
        }

        /// <summary>
        /// Gets the value of the Dock attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The Dock attached property.</returns>
        public static Dock GetDock(Control control)
        {
            return control.GetValue(DockProperty);
        }

        /// <summary>
        /// Sets the value of the Dock attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the Dock property.</param>
        public static void SetDock(Control control, Dock value)
        {
            control.SetValue(DockProperty, value);
        }

        /// <summary>
        /// Gets or sets a value which indicates whether the last child of the 
        /// <see cref="DockPanel"/> fills the remaining space in the panel.
        /// </summary>
        public bool LastChildFill
        {
            get => GetValue(LastChildFillProperty);
            set => SetValue(LastChildFillProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal distance between the child objects.
        /// </summary>
        public double HorizontalSpacing
        {
            get => GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical distance between the child objects.
        /// </summary>
        public double VerticalSpacing
        {
            get => GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }


        /// <summary>
        /// Updates DesiredSize of the DockPanel.  Called by parent Control.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Children are measured based on their sizing properties and <see cref="Dock" />.  
        /// Each child is allowed to consume all the space on the side on which it is docked; Left/Right docked
        /// children are granted all vertical space for their entire width, and Top/Bottom docked children are
        /// granted all horizontal space for their entire height.
        /// </remarks>
        /// <param name="availableSize">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Panel's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var parentWidth = 0d;
            var parentHeight = 0d;
            var accumulatedWidth = 0d;
            var accumulatedHeight = 0d;

            var horizontalSpacing = false;
            var verticalSpacing = false;
            var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

            for (var index = 0; index < childrenCount; ++index)
            {
                var child = Children[index];
                var childConstraint = new Size(
                    Math.Max(0, availableSize.Width - accumulatedWidth),
                    Math.Max(0, availableSize.Height - accumulatedHeight));

                child.Measure(childConstraint);
                var childDesiredSize = child.DesiredSize;

                // Now, we adjust:
                // 1. Size consumed by children (accumulatedSize).  This will be used when computing subsequent
                //    children to determine how much space is remaining for them.
                // 2. Parent size implied by this child (parentSize) when added to the current children (accumulatedSize).
                //    This is different from the size above in one respect: A Dock.Left child implies a height, but does
                //    not actually consume any height for subsequent children.
                // If we accumulate size in a given dimension, the next child (or the end conditions after the child loop)
                // will deal with computing our minimum size (parentSize) due to that accumulation.
                // Therefore, we only need to compute our minimum size (parentSize) in dimensions that this child does
                //   not accumulate: Width for Top/Bottom, Height for Left/Right.
                switch (child.GetValue(DockProperty))
                {
                    case Dock.Left:
                    case Dock.Right:
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                        if (child.IsVisible)
                        {
                            accumulatedWidth += HorizontalSpacing;
                            horizontalSpacing = true;
                        }
                        accumulatedWidth += childDesiredSize.Width;
                        break;

                    case Dock.Top:
                    case Dock.Bottom:
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                        if (child.IsVisible)
                        {
                            accumulatedHeight += VerticalSpacing;
                            verticalSpacing = true;
                        }
                        accumulatedHeight += childDesiredSize.Height;
                        break;
                }
            }

            if (LastChildFill && Children.Count > 0)
            {
                var child = Children[Children.Count - 1];
                var childConstraint = new Size(
                    Math.Max(0, availableSize.Width - accumulatedWidth),
                    Math.Max(0, availableSize.Height - accumulatedHeight));

                child.Measure(childConstraint);
                var childDesiredSize = child.DesiredSize;
                parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                accumulatedHeight += childDesiredSize.Height;
                accumulatedWidth += childDesiredSize.Width;
            }
            else
            {
                if (horizontalSpacing)
                    accumulatedWidth -= HorizontalSpacing;
                if (verticalSpacing)
                    accumulatedHeight -= VerticalSpacing;
            }

            // Make sure the final accumulated size is reflected in parentSize.
            parentWidth = Math.Max(parentWidth, accumulatedWidth);
            parentHeight = Math.Max(parentHeight, accumulatedHeight);
            return new Size(parentWidth, parentHeight);
        }

        /// <summary>
        /// DockPanel computes a position and final size for each of its children based upon their
        /// <see cref="Dock" /> enum and sizing properties.
        /// </summary>
        /// <param name="finalSize">Size that DockPanel will assume to position children.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count is 0)
                return finalSize;

            var currentBounds = new Rect(finalSize);
            var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

            for (var index = 0; index < childrenCount; ++index)
            {
                var child = Children[index];
                if (!child.IsVisible)
                    continue;

                var dock = child.GetValue(DockProperty);
                double width, height;
                switch (dock)
                {
                    case Dock.Left:

                        width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                        child.Arrange(currentBounds.WithWidth(width));
                        width += HorizontalSpacing;
                        currentBounds = new Rect(currentBounds.X + width, currentBounds.Y, Math.Max(0, currentBounds.Width - width), currentBounds.Height);

                        break;
                    case Dock.Top:

                        height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                        child.Arrange(currentBounds.WithHeight(height));
                        height += VerticalSpacing;
                        currentBounds = new Rect(currentBounds.X, currentBounds.Y + height, currentBounds.Width, Math.Max(0, currentBounds.Height - height));

                        break;
                    case Dock.Right:

                        width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                        child.Arrange(new Rect(currentBounds.X + currentBounds.Width - width, currentBounds.Y, width, currentBounds.Height));
                        width += HorizontalSpacing;
                        currentBounds = currentBounds.WithWidth(Math.Max(0, currentBounds.Width - width));

                        break;
                    case Dock.Bottom:

                        height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                        child.Arrange(new Rect(currentBounds.X, currentBounds.Y + currentBounds.Height - height, currentBounds.Width, height));
                        height += VerticalSpacing;
                        currentBounds = currentBounds.WithHeight(Math.Max(0, currentBounds.Height - height));

                        break;
                }
            }

            if (LastChildFill && Children.Count > 0)
            {
                var child = Children[Children.Count - 1];
                child.Arrange(new Rect(currentBounds.X, currentBounds.Y, currentBounds.Width, currentBounds.Height));
            }

            return finalSize;
        }
    }
}
