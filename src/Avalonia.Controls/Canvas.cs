// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel that displays child controls at arbitrary locations.
    /// </summary>
    /// <remarks>
    /// Unlike other <see cref="Panel"/> implementations, the <see cref="Canvas"/> doesn't lay out
    /// its children in any particular layout. Instead, the positioning of each child control is
    /// defined by the <code>Canvas.Left</code>, <code>Canvas.Top</code>, <code>Canvas.Right</code>
    /// and <code>Canvas.Bottom</code> attached properties.
    /// </remarks>
    public class Canvas : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the Left attached property.
        /// </summary>
        public static readonly AttachedProperty<double> LeftProperty =
            AvaloniaProperty.RegisterAttached<Canvas, Control, double>("Left", double.NaN);

        /// <summary>
        /// Defines the Top attached property.
        /// </summary>
        public static readonly AttachedProperty<double> TopProperty =
            AvaloniaProperty.RegisterAttached<Canvas, Control, double>("Top", double.NaN);

        /// <summary>
        /// Defines the Right attached property.
        /// </summary>
        public static readonly AttachedProperty<double> RightProperty =
            AvaloniaProperty.RegisterAttached<Canvas, Control, double>("Right", double.NaN);

        /// <summary>
        /// Defines the Bottom attached property.
        /// </summary>
        public static readonly AttachedProperty<double> BottomProperty =
            AvaloniaProperty.RegisterAttached<Canvas, Control, double>("Bottom", double.NaN);

        /// <summary>
        /// Initializes static members of the <see cref="Canvas"/> class.
        /// </summary>
        static Canvas()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Canvas>(false);
            AffectsCanvasArrange(LeftProperty, TopProperty, RightProperty, BottomProperty);
        }

        /// <summary>
        /// Gets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static double GetLeft(AvaloniaObject element)
        {
            return element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Sets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetLeft(AvaloniaObject element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        /// <summary>
        /// Gets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's top coordinate.</returns>
        public static double GetTop(AvaloniaObject element)
        {
            return element.GetValue(TopProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The top value.</param>
        public static void SetTop(AvaloniaObject element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        /// <summary>
        /// Gets the value of the Right attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's right coordinate.</returns>
        public static double GetRight(AvaloniaObject element)
        {
            return element.GetValue(RightProperty);
        }

        /// <summary>
        /// Sets the value of the Right attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The right value.</param>
        public static void SetRight(AvaloniaObject element, double value)
        {
            element.SetValue(RightProperty, value);
        }

        /// <summary>
        /// Gets the value of the Bottom attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's bottom coordinate.</returns>
        public static double GetBottom(AvaloniaObject element)
        {
            return element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Sets the value of the Bottom attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The bottom value.</param>
        public static void SetBottom(AvaloniaObject element, double value)
        {
            element.SetValue(BottomProperty, value);
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
            // TODO: Implement this
            return null;
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (Control child in Children)
            {
                child.Measure(availableSize);
            }

            return new Size();
        }

        /// <summary>
        /// Arranges the control's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (Control child in Children)
            {
                double x = 0.0;
                double y = 0.0;
                double elementLeft = GetLeft(child);

                if (!double.IsNaN(elementLeft))
                {
                    x = elementLeft;
                }
                else
                {
                    // Arrange with right.
                    double elementRight = GetRight(child);
                    if (!double.IsNaN(elementRight))
                    {
                        x = finalSize.Width - child.DesiredSize.Width - elementRight;
                    }
                }

                double elementTop = GetTop(child);
                if (!double.IsNaN(elementTop) )
                {
                    y = elementTop;
                }
                else
                {
                    double elementBottom = GetBottom(child);
                    if (!double.IsNaN(elementBottom))
                    {
                        y = finalSize.Height - child.DesiredSize.Height - elementBottom;
                    }
                }

                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return finalSize;
        }

        /// <summary>
        /// Marks a property on a child as affecting the canvas' arrangement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        private static void AffectsCanvasArrange(params AvaloniaProperty[] properties)
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(AffectsCanvasArrangeInvalidate);
            }
        }

        /// <summary>
        /// Calls <see cref="Layoutable.InvalidateArrange"/> on the parent of the control whose
        /// property changed, if that parent is a canvas.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsCanvasArrangeInvalidate(AvaloniaPropertyChangedEventArgs e)
        {
            var control = e.Sender as IControl;
            var canvas = control?.VisualParent as Canvas;
            canvas?.InvalidateArrange();
        }
    }
}
