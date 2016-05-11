namespace Avalonia.Controls
{
    using System;

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
                nameof(LastChildFillProperty),
                defaultValue: true);

        /// <summary>
        /// Initializes static members of the <see cref="DockPanel"/> class.
        /// </summary>
        static DockPanel()
        {
            AffectsArrange(DockProperty);
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
            get { return GetValue(LastChildFillProperty); }
            set { SetValue(LastChildFillProperty, value); }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            double usedWidth = 0.0;
            double usedHeight = 0.0;
            double maximumWidth = 0.0;
            double maximumHeight = 0.0;

            // Measure each of the Children
            foreach (Control element in Children)
            {
                // Get the child's desired size
                Size remainingSize = new Size(
                    Math.Max(0.0, constraint.Width - usedWidth),
                    Math.Max(0.0, constraint.Height - usedHeight));
                element.Measure(remainingSize);
                Size desiredSize = element.DesiredSize;

                // Decrease the remaining space for the rest of the children
                switch (GetDock(element))
                {
                    case Dock.Left:
                    case Dock.Right:
                        maximumHeight = Math.Max(maximumHeight, usedHeight + desiredSize.Height);
                        usedWidth += desiredSize.Width;
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        maximumWidth = Math.Max(maximumWidth, usedWidth + desiredSize.Width);
                        usedHeight += desiredSize.Height;
                        break;
                }
            }

            maximumWidth = Math.Max(maximumWidth, usedWidth);
            maximumHeight = Math.Max(maximumHeight, usedHeight);
            return new Size(maximumWidth, maximumHeight);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double left = 0.0;
            double top = 0.0;
            double right = 0.0;
            double bottom = 0.0;

            // Arrange each of the Children
            var children = Children;
            int dockedCount = children.Count - (LastChildFill ? 1 : 0);
            int index = 0;

            foreach (Control element in children)
            {
                // Determine the remaining space left to arrange the element
                Rect remainingRect = new Rect(
                    left,
                    top,
                    Math.Max(0.0, arrangeSize.Width - left - right),
                    Math.Max(0.0, arrangeSize.Height - top - bottom));

                // Trim the remaining Rect to the docked size of the element
                // (unless the element should fill the remaining space because
                // of LastChildFill)
                if (index < dockedCount)
                {
                    Size desiredSize = element.DesiredSize;
                    switch (GetDock(element))
                    {
                        case Dock.Left:
                            left += desiredSize.Width;
                            remainingRect = remainingRect.WithWidth(desiredSize.Width);
                            break;
                        case Dock.Top:
                            top += desiredSize.Height;
                            remainingRect = remainingRect.WithHeight(desiredSize.Height);
                            break;
                        case Dock.Right:
                            right += desiredSize.Width;
                            remainingRect = new Rect(
                                Math.Max(0.0, arrangeSize.Width - right),
                                remainingRect.Y,
                                desiredSize.Width,
                                remainingRect.Height);
                            break;
                        case Dock.Bottom:
                            bottom += desiredSize.Height;
                            remainingRect = new Rect(
                                remainingRect.X,
                                Math.Max(0.0, arrangeSize.Height - bottom),
                                remainingRect.Width,
                                desiredSize.Height);
                            break;
                    }
                }

                element.Arrange(remainingRect);
                index++;
            }

            return arrangeSize;
        }
    }
}