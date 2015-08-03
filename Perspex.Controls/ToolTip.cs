// -----------------------------------------------------------------------
// <copyright file="ToolTip.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Threading;

    /// <summary>
    /// A tooltip control.
    /// </summary>
    /// <remarks>
    /// You will probably not want to create a <see cref="ToolTip"/> control directly: if added to 
    /// the tree it will act as a simple <see cref="ContentControl"/> styled to look like a tooltip.
    /// To add a tooltip to a control, use the <see cref="TipProperty"/> attached property, 
    /// assigning the content that you want displayed.
    /// </remarks>
    public class ToolTip : ContentControl
    {
        /// <summary>
        /// Defines the ToolTip.Tip attached property.
        /// </summary>
        public static readonly PerspexProperty<object> TipProperty =
            PerspexProperty.RegisterAttached<ToolTip, Control, object>("Tip");

        /// <summary>
        /// The popup window used to display the active tooltip.
        /// </summary>
        private static PopupRoot popup;

        /// <summary>
        /// The control that the currently visible tooltip is attached to.
        /// </summary>
        private static Control current;

        /// <summary>
        /// Observable fired when a tooltip should be displayed for a control. The output from this
        /// observable is throttled and calls <see cref="ShowToolTip(Control)"/> when the time 
        /// period expires.
        /// </summary>
        private static Subject<Control> show = new Subject<Control>();

        /// <summary>
        /// Statically constructs the tooltip class.
        /// </summary>
        static ToolTip()
        {
            TipProperty.Changed.Subscribe(TipChanged);
            show.Throttle(TimeSpan.FromSeconds(0.5), PerspexScheduler.Instance).Subscribe(ShowToolTip);
        }

        /// <summary>
        /// Gets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// The content to be displayed in the control's tooltip.
        /// </returns>
        public static object GetTip(Control element)
        {
            return element.GetValue(TipProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">The content to be displayed in the control's tooltip.</param>
        public static void SetTip(Control element, object value)
        {
            element.SetValue(TipProperty, value);
        }

        /// <summary>
        /// called when the <see cref="TipProperty"/> property changes on a control.
        /// </summary>
        /// <param name="e"></param>
        private static void TipChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue != null)
            {
                control.PointerEnter -= ControlPointerEnter;
                control.PointerLeave -= ControlPointerLeave;
            }

            if (e.NewValue != null)
            {
                control.PointerEnter += ControlPointerEnter;
                control.PointerLeave += ControlPointerLeave;
            }
        }

        /// <summary>
        /// Shows a tooltip for the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        private static void ShowToolTip(Control control)
        {
            if (control != null)
            {
                if (popup == null)
                {
                    popup = new PopupRoot
                    {
                        Content = new ToolTip(),
                    };

                    ((ISetLogicalParent)popup).SetParent(control);
                }

                var cp = MouseDevice.Instance?.GetPosition(control);
                var position = control.PointToScreen(cp.HasValue ? cp.Value : new Point(0, 0)) + new Vector(0, 22);

                ((ToolTip)popup.Content).Content = GetTip(control);
                popup.SetPosition(position);
                popup.Show();

                current = control;
            }
        }

        /// <summary>
        /// Called when the pointer enters a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void ControlPointerEnter(object sender, PointerEventArgs e)
        {
            current = (Control)sender;
            show.OnNext(current);
        }

        /// <summary>
        /// Called when the pointer leaves a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void ControlPointerLeave(object sender, PointerEventArgs e)
        {
            var control = (Control)sender;

            if (control == current)
            {
                if (popup != null && popup.IsVisible)
                {
                    popup.Hide();
                }

                show.OnNext(null);
            }
        }
    }
}
