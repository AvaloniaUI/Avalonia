// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which pops up a hint when a control is hovered.
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
        public static readonly AttachedProperty<object> TipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, object>("Tip");

        /// <summary>
        /// The popup window used to display the active tooltip.
        /// </summary>
        private static PopupRoot s_popup;

        /// <summary>
        /// The control that the currently visible tooltip is attached to.
        /// </summary>
        private static Control s_current;

        /// <summary>
        /// Observable fired when a tooltip should be displayed for a control. The output from this
        /// observable is throttled and calls <see cref="ShowToolTip(Control)"/> when the time
        /// period expires.
        /// </summary>
        private static readonly Subject<Control> s_show = new Subject<Control>();

        /// <summary>
        /// Initializes static members of the <see cref="ToolTip"/> class.
        /// </summary>
        static ToolTip()
        {
            TipProperty.Changed.Subscribe(TipChanged);
            s_show.Throttle(TimeSpan.FromSeconds(0.5), AvaloniaScheduler.Instance).Subscribe(ShowToolTip);
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
        /// <param name="e">The event args.</param>
        private static void TipChanged(AvaloniaPropertyChangedEventArgs e)
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
            if (control != null && control.IsVisible && control.GetVisualRoot() != null)
            {
                var cp = (control.GetVisualRoot() as IInputRoot)?.MouseDevice?.GetPosition(control);

                if (cp.HasValue && control.IsVisible && new Rect(control.Bounds.Size).Contains(cp.Value))
                {
                    var position = control.PointToScreen(cp.Value) + new Vector(0, 22);

                    if (s_popup == null)
                    {
                        s_popup = new PopupRoot();
                        s_popup.Content = new ToolTip();
                    }
                    else
                    {
                        ((ISetLogicalParent)s_popup).SetParent(null);
                    }

                ((ISetLogicalParent)s_popup).SetParent(control);
                    ((ToolTip)s_popup.Content).Content = GetTip(control);
                    s_popup.Position = position;
                    s_popup.Show();

                    s_current = control;
                }
            }
        }

        /// <summary>
        /// Called when the pointer enters a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void ControlPointerEnter(object sender, PointerEventArgs e)
        {
            s_current = (Control)sender;
            s_show.OnNext(s_current);
        }

        /// <summary>
        /// Called when the pointer leaves a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void ControlPointerLeave(object sender, PointerEventArgs e)
        {
            var control = (Control)sender;

            if (control == s_current)
            {
                if (s_popup != null)
                {
                    DisposeTooltip();
                    s_show.OnNext(null);
                }
            }
        }

        private static void DisposeTooltip()
        {
            if (s_popup != null)
            {
                // Clear the ToolTip's Content in case it has control content: this will
                // reset its visual parent allowing it to be used again.
                ((ToolTip)s_popup.Content).Content = null;

                // Dispose of the popup.
                s_popup.Dispose();
                s_popup = null;
            }
        }
    }
}
