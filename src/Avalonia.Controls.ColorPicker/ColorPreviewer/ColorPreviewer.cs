using System;
using System.Globalization;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Presents a preview color with optional accent colors.
    /// </summary>
    [TemplatePart("PART_AccentDecrement1Border", typeof(Border))]
    [TemplatePart("PART_AccentDecrement2Border", typeof(Border))]
    [TemplatePart("PART_AccentIncrement1Border", typeof(Border))]
    [TemplatePart("PART_AccentIncrement2Border", typeof(Border))]
    public partial class ColorPreviewer : TemplatedControl
    {
        /// <summary>
        /// Event for when the selected color changes within the previewer.
        /// This occurs when an accent color is pressed.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        private bool eventsConnected = false;

        // XAML template parts
        private Border? _accentDecrement1Border;
        private Border? _accentDecrement2Border;
        private Border? _accentIncrement1Border;
        private Border? _accentIncrement2Border;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPreviewer"/> class.
        /// </summary>
        public ColorPreviewer() : base()
        {
        }

        /// <summary>
        /// Connects or disconnects all control event handlers.
        /// </summary>
        /// <param name="connected">True to connect event handlers, otherwise false.</param>
        private void ConnectEvents(bool connected)
        {
            if (connected == true && eventsConnected == false)
            {
                // Add all events
                if (_accentDecrement1Border != null) { _accentDecrement1Border.PointerPressed += AccentBorder_PointerPressed; }
                if (_accentDecrement2Border != null) { _accentDecrement2Border.PointerPressed += AccentBorder_PointerPressed; }
                if (_accentIncrement1Border != null) { _accentIncrement1Border.PointerPressed += AccentBorder_PointerPressed; }
                if (_accentIncrement2Border != null) { _accentIncrement2Border.PointerPressed += AccentBorder_PointerPressed; }

                eventsConnected = true;
            }
            else if (connected == false && eventsConnected == true)
            {
                // Remove all events
                if (_accentDecrement1Border != null) { _accentDecrement1Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (_accentDecrement2Border != null) { _accentDecrement2Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (_accentIncrement1Border != null) { _accentIncrement1Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (_accentIncrement2Border != null) { _accentIncrement2Border.PointerPressed -= AccentBorder_PointerPressed; }

                eventsConnected = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // Remove any existing events present if the control was previously loaded then unloaded
            ConnectEvents(false);

            _accentDecrement1Border = e.NameScope.Find<Border>("PART_AccentDecrement1Border");
            _accentDecrement2Border = e.NameScope.Find<Border>("PART_AccentDecrement2Border");
            _accentIncrement1Border = e.NameScope.Find<Border>("PART_AccentIncrement1Border");
            _accentIncrement2Border = e.NameScope.Find<Border>("PART_AccentIncrement2Border");

            // Must connect after controls are found
            ConnectEvents(true);

            base.OnApplyTemplate(e);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == HsvColorProperty)
            {
                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<HsvColor>().ToRgb(),
                    change.GetNewValue<HsvColor>().ToRgb()));
            }

            base.OnPropertyChanged(change);
        }

        /// <summary>
        /// Called before the <see cref="ColorChanged"/> event occurs.
        /// </summary>
        /// <param name="e">The <see cref="ColorChangedEventArgs"/> defining old/new colors.</param>
        protected virtual void OnColorChanged(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Event handler for when an accent color border is pressed.
        /// This will update the color to the background of the pressed panel.
        /// </summary>
        private void AccentBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Border? border = sender as Border;
            int accentStep = 0;
            HsvColor hsvColor = HsvColor;

            // Get the value component delta
            try
            {
                accentStep = int.Parse(border?.Tag?.ToString() ?? "0", CultureInfo.InvariantCulture);
            }
            catch { }

            if (accentStep != 0)
            {
                // ColorChanged will be invoked in OnPropertyChanged if the value is different
                SetCurrentValue(HsvColorProperty, AccentColorConverter.GetAccent(hsvColor, accentStep));
            }
        }
    }
}
