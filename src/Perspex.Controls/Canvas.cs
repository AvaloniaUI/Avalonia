// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input;

namespace Perspex.Controls
{
    public class Canvas : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the <see cref="Left"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> LeftProperty =
            PerspexProperty.RegisterAttached<StackPanel, Control, double>("Left");

        /// <summary>
        /// Defines the <see cref="Top"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> TopProperty =
            PerspexProperty.RegisterAttached<StackPanel, Control, double>("Top");

        /// <summary>
        /// Initializes static members of the <see cref="Canvas"/> class.
        /// </summary>
        static Canvas()
        {
            AffectsArrange(LeftProperty);
            AffectsArrange(TopProperty);
        }

        /// <summary>
        /// Gets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static double GetLeft(PerspexObject element)
        {
            return element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Sets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetLeft(PerspexObject element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        /// <summary>
        /// Gets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's top coordinate.</returns>
        public static double GetTop(PerspexObject element)
        {
            return element.GetValue(TopProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The top value.</param>
        public static void SetTop(PerspexObject element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        IInputElement INavigableContainer.GetControl(FocusNavigationDirection direction, IInputElement from)
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

                if (double.IsNaN(elementLeft) == false)
                {
                    x = elementLeft;
                }

                double elementTop = GetTop(child);
                if (double.IsNaN(elementTop) == false)
                {
                    y = elementTop;
                }

                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return finalSize;
        }
    }
}
