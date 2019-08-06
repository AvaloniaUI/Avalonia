// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
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
        /// Defines the ToolTip.IsOpen attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsOpenProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, bool>("IsOpen");

        /// <summary>
        /// Defines the ToolTip.Placement property.
        /// </summary>
        public static readonly AttachedProperty<PlacementMode> PlacementProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, PlacementMode>("Placement", defaultValue: PlacementMode.Pointer);

        /// <summary>
        /// Defines the ToolTip.HorizontalOffset property.
        /// </summary>
        public static readonly AttachedProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, double>("HorizontalOffset");

        /// <summary>
        /// Defines the ToolTip.VerticalOffset property.
        /// </summary>
        public static readonly AttachedProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, double>("VerticalOffset", 20);

        /// <summary>
        /// Defines the ToolTip.ShowDelay property.
        /// </summary>
        public static readonly AttachedProperty<int> ShowDelayProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, int>("ShowDelay", 400);

        /// <summary>
        /// Stores the current <see cref="ToolTip"/> instance in the control.
        /// </summary>
        private static readonly AttachedProperty<ToolTip> ToolTipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, ToolTip>("ToolTip");

        private IPopupHost _popup;

        /// <summary>
        /// Initializes static members of the <see cref="ToolTip"/> class.
        /// </summary>
        static ToolTip()
        {
            TipProperty.Changed.Subscribe(ToolTipService.Instance.TipChanged);
            IsOpenProperty.Changed.Subscribe(IsOpenChanged);
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
        /// Gets the value of the ToolTip.IsOpen attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating whether the tool tip is visible.
        /// </returns>
        public static bool GetIsOpen(Control element)
        {
            return element.GetValue(IsOpenProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.IsOpen attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating whether the tool tip is visible.</param>
        public static void SetIsOpen(Control element, bool value)
        {
            element.SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.Placement attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static PlacementMode GetPlacement(Control element)
        {
            return element.GetValue(PlacementProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Placement attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetPlacement(Control element, PlacementMode value)
        {
            element.SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.HorizontalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static double GetHorizontalOffset(Control element)
        {
            return element.GetValue(HorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.HorizontalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetHorizontalOffset(Control element, double value)
        {
            element.SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.VerticalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static double GetVerticalOffset(Control element)
        {
            return element.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.VerticalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetVerticalOffset(Control element, double value)
        {
            element.SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.ShowDelay attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating the time, in milliseconds, before a tool tip opens.
        /// </returns>
        public static int GetShowDelay(Control element)
        {
            return element.GetValue(ShowDelayProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.ShowDelay attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating the time, in milliseconds, before a tool tip opens.</param>
        public static void SetShowDelay(Control element, int value)
        {
            element.SetValue(ShowDelayProperty, value);
        }

        private static void IsOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if ((bool)e.NewValue)
            {
                var tip = GetTip(control);
                if (tip == null) return;

                var toolTip = control.GetValue(ToolTipProperty);
                if (toolTip == null || (tip != toolTip && tip != toolTip.Content))
                {
                    toolTip?.Close();

                    toolTip = tip as ToolTip ?? new ToolTip { Content = tip };
                    control.SetValue(ToolTipProperty, toolTip);
                }

                toolTip.Open(control);
            }
            else
            {
                var toolTip = control.GetValue(ToolTipProperty);
                toolTip?.Close();
            }
        }

        private void Open(Control control)
        {
            Close();

            _popup = OverlayPopupHost.CreatePopupHost(control, null);
            _popup.SetChild(this);
            ((ISetLogicalParent)_popup).SetParent(control);
            
            _popup.ConfigurePosition(control, GetPlacement(control), 
                new Point(GetHorizontalOffset(control), GetVerticalOffset(control)));
            _popup.Show();
        }

        private void Close()
        {
            if (_popup != null)
            {
                _popup.SetChild(null);
                _popup.Hide();
                _popup = null;
            }
        }
    }
}
