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
    [TemplatePart(Name = nameof(AccentDec1Border), Type = typeof(Border))]
    [TemplatePart(Name = nameof(AccentDec2Border), Type = typeof(Border))]
    [TemplatePart(Name = nameof(AccentInc1Border), Type = typeof(Border))]
    [TemplatePart(Name = nameof(AccentInc2Border), Type = typeof(Border))]
    public partial class ColorPreviewer : TemplatedControl
    {
        /// <summary>
        /// Event for when the selected color changes within the previewer.
        /// This occurs when an accent color is pressed.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        private bool eventsConnected = false;

        private Border? AccentDec1Border;
        private Border? AccentDec2Border;
        private Border? AccentInc1Border;
        private Border? AccentInc2Border;

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
                if (AccentDec1Border != null) { AccentDec1Border.PointerPressed += AccentBorder_PointerPressed; }
                if (AccentDec2Border != null) { AccentDec2Border.PointerPressed += AccentBorder_PointerPressed; }
                if (AccentInc1Border != null) { AccentInc1Border.PointerPressed += AccentBorder_PointerPressed; }
                if (AccentInc2Border != null) { AccentInc2Border.PointerPressed += AccentBorder_PointerPressed; }

                eventsConnected = true;
            }
            else if (connected == false && eventsConnected == true)
            {
                // Remove all events
                if (AccentDec1Border != null) { AccentDec1Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (AccentDec2Border != null) { AccentDec2Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (AccentInc1Border != null) { AccentInc1Border.PointerPressed -= AccentBorder_PointerPressed; }
                if (AccentInc2Border != null) { AccentInc2Border.PointerPressed -= AccentBorder_PointerPressed; }

                eventsConnected = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // Remove any existing events present if the control was previously loaded then unloaded
            ConnectEvents(false);

            AccentDec1Border = e.NameScope.Find<Border>(nameof(AccentDec1Border));
            AccentDec2Border = e.NameScope.Find<Border>(nameof(AccentDec2Border));
            AccentInc1Border = e.NameScope.Find<Border>(nameof(AccentInc1Border));
            AccentInc2Border = e.NameScope.Find<Border>(nameof(AccentInc2Border));

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
                accentStep = int.Parse(border?.Tag?.ToString() ?? "", CultureInfo.InvariantCulture);
            }
            catch { }

            HsvColor newHsvColor = AccentColorConverter.GetAccent(hsvColor, accentStep);
            HsvColor oldHsvColor = HsvColor;

            HsvColor = newHsvColor;
            OnColorChanged(new ColorChangedEventArgs(oldHsvColor.ToRgb(), newHsvColor.ToRgb()));
        }
    }
}
