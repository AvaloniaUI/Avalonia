using System;
using System.ComponentModel;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Reactive;
using Avalonia.Styling;

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
    [PseudoClasses(":open")]
    public class ToolTip : ContentControl, IPopupHostProvider
    {
        /// <summary>
        /// Defines the ToolTip.Tip attached property.
        /// </summary>
        public static readonly AttachedProperty<object?> TipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, object?>("Tip");

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

        /// <inheritdoc cref="Popup.CustomPopupPlacementCallbackProperty"/>
        public static readonly StyledProperty<CustomPopupPlacementCallback?> CustomPopupPlacementCallbackProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, CustomPopupPlacementCallback?>("CustomPopupPlacementCallback");

        /// <summary>
        /// Defines the ToolTip.ShowDelay property.
        /// </summary>
        public static readonly AttachedProperty<int> ShowDelayProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, int>("ShowDelay", 400);

        /// <summary>
        /// Defines the ToolTip.BetweenShowDelay property.
        /// </summary>
        public static readonly AttachedProperty<int> BetweenShowDelayProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, int>("BetweenShowDelay", 100);

        /// <summary>
        /// Defines the ToolTip.ShowOnDisabled property.
        /// </summary>
        public static readonly AttachedProperty<bool> ShowOnDisabledProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, bool>("ShowOnDisabled", defaultValue: false, inherits: true);

        /// <summary>
        /// Defines the ToolTip.ServiceEnabled property.
        /// </summary>
        public static readonly AttachedProperty<bool> ServiceEnabledProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, bool>("ServiceEnabled", defaultValue: true, inherits: true);

        /// <summary>
        /// Stores the current <see cref="ToolTip"/> instance in the control.
        /// </summary>
        internal static readonly AttachedProperty<ToolTip?> ToolTipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, ToolTip?>("ToolTip");

        private Popup? _popup;
        private Action<IPopupHost?>? _popupHostChangedHandler;
        private CompositeDisposable? _subscriptions;

        /// <summary>
        /// Initializes static members of the <see cref="ToolTip"/> class.
        /// </summary>
        static ToolTip()
        {
            IsOpenProperty.Changed.Subscribe(IsOpenChanged);
        }

        internal Control? AdornedControl { get; private set; }
        internal event EventHandler? Closed;

        /// <summary>
        /// Gets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// The content to be displayed in the control's tooltip.
        /// </returns>
        public static object? GetTip(Control element)
        {
            return element.GetValue(TipProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">The content to be displayed in the control's tooltip.</param>
        public static void SetTip(Control element, object? value)
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

        /// <summary>
        /// Gets the number of milliseconds since the last tooltip closed during which the tooltip of <paramref name="element"/> will open immediately,
        /// or a negative value indicating that the tooltip will always wait for <see cref="ShowDelayProperty"/> before opening.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        public static int GetBetweenShowDelay(Control element) => element.GetValue(BetweenShowDelayProperty);

        /// <summary>
        /// Sets the number of milliseconds since the last tooltip closed during which the tooltip of <paramref name="element"/> will open immediately.
        /// </summary>
        /// <remarks>
        /// Setting a negative value disables the immediate opening behaviour. The tooltip of <paramref name="element"/> will then always wait until 
        /// <see cref="ShowDelayProperty"/> elapses before showing.
        /// </remarks>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">The number of milliseconds to set, or a negative value to disable the behaviour.</param>
        public static void SetBetweenShowDelay(Control element, int value) => element.SetValue(BetweenShowDelayProperty, value);

        /// <summary>
        /// Gets whether a control will display a tooltip even if it disabled.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        public static bool GetShowOnDisabled(Control element) =>
            element.GetValue(ShowOnDisabledProperty);

        /// <summary>
        /// Sets whether a control will display a tooltip even if it disabled.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">Whether the control is to display a tooltip even if it disabled.</param>
        public static void SetShowOnDisabled(Control element, bool value) => 
            element.SetValue(ShowOnDisabledProperty, value);

        /// <summary>
        /// Gets whether showing and hiding of a control's tooltip will be automatically controlled by Avalonia.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        public static bool GetServiceEnabled(Control element) =>
            element.GetValue(ServiceEnabledProperty);

        /// <summary>
        /// Sets whether showing and hiding of a control's tooltip will be automatically controlled by Avalonia.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">Whether the control is to display a tooltip even if it disabled.</param>
        public static void SetServiceEnabled(Control element, bool value) => 
            element.SetValue(ServiceEnabledProperty, value);

        /// <summary>
        /// Gets the value of the ToolTip.CustomPopupPlacementCallback attached property.
        /// </summary>
        public static CustomPopupPlacementCallback? GetCustomPopupPlacementCallback(Control element)
        {
            return element.GetValue(CustomPopupPlacementCallbackProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.CustomPopupPlacementCallback attached property.
        /// </summary>
        public static void SetCustomPopupPlacementCallback(Control element, CustomPopupPlacementCallback? value)
        {
            element.SetValue(CustomPopupPlacementCallbackProperty, value);
        }

        private static void IsOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var newValue = (bool)e.NewValue!;

            if (newValue)
            {
                var tip = GetTip(control);
                if (tip == null) return;

                var toolTip = control.GetValue(ToolTipProperty);
                if (toolTip == null || (tip != toolTip && tip != toolTip.Content))
                {
                    toolTip?.Close();

                    toolTip = tip as ToolTip ?? new ToolTip { Content = tip };
                    control.SetValue(ToolTipProperty, toolTip);
                    toolTip.SetValue(ThemeVariant.RequestedThemeVariantProperty, control.ActualThemeVariant);
                }

                toolTip.AdornedControl = control;
                toolTip.Open(control);
                toolTip?.UpdatePseudoClasses(newValue);
            }
            else if (control.GetValue(ToolTipProperty) is { } toolTip)
            {
                toolTip.AdornedControl = null;
                toolTip.Close();
                toolTip?.UpdatePseudoClasses(newValue);
            }
        }

        IPopupHost? IPopupHostProvider.PopupHost => _popup?.Host;

        internal IPopupHost? PopupHost => _popup?.Host;

        event Action<IPopupHost?>? IPopupHostProvider.PopupHostChanged 
        { 
            add => _popupHostChangedHandler += value; 
            remove => _popupHostChangedHandler -= value;
        }

        private void Open(Control control)
        {
            Close();
            
            if (_popup is null)
            {
                _popup = new Popup();
                _popup.Child = this;
                _popup.WindowManagerAddShadowHint = false;

                _popup.Opened += OnPopupOpened;
                _popup.Closed += OnPopupClosed;
            }

            _subscriptions = new CompositeDisposable(new[]
            {
                _popup.Bind(Popup.HorizontalOffsetProperty, control.GetBindingObservable(HorizontalOffsetProperty)),
                _popup.Bind(Popup.VerticalOffsetProperty, control.GetBindingObservable(VerticalOffsetProperty)),
                _popup.Bind(Popup.PlacementProperty, control.GetBindingObservable(PlacementProperty)),
                _popup.Bind(Popup.CustomPopupPlacementCallbackProperty, control.GetBindingObservable(CustomPopupPlacementCallbackProperty))
            });

            _popup.PlacementTarget = control;
            _popup.SetPopupParent(control);

            _popup.IsOpen = true;
        }

        private void Close()
        {
            _subscriptions?.Dispose();

            if (_popup is not null)
            {
                _popup.IsOpen = false;
                _popup.SetPopupParent(null);
                _popup.PlacementTarget = null;
            }
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            // This condition is true, when Popup was closed by any other reason outside of ToolTipService/ToolTip, keeping IsOpen=true.
            if (AdornedControl is { } adornedControl
                && GetIsOpen(adornedControl))
            {
                adornedControl.SetCurrentValue(IsOpenProperty, false);
            }

            _popupHostChangedHandler?.Invoke(null);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            _popupHostChangedHandler?.Invoke(((Popup)sender!).Host);
        }

        private void UpdatePseudoClasses(bool newValue)
        {
            PseudoClasses.Set(":open", newValue);
        }
    }
}
