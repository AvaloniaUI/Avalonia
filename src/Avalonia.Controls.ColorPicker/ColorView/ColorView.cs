using System;
using System.Collections.Generic;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders.
    /// </summary>
    [TemplatePart("PART_HexTextBox", typeof(TextBox))]
    public partial class ColorView : TemplatedControl
    {
        /// <summary>
        /// Event for when the selected color changes within the slider.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        // XAML template parts
        private TextBox?    _hexTextBox;

        protected bool _ignorePropertyChanged = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorView"/> class.
        /// </summary>
        public ColorView() : base()
        {
        }

        /// <summary>
        /// Gets the value of the hex TextBox and sets it as the current <see cref="Color"/>.
        /// If invalid, the TextBox hex text will revert back to the last valid color.
        /// </summary>
        private void GetColorFromHexTextBox()
        {
            if (_hexTextBox != null)
            {
                var convertedColor = ColorToHexConverter.ParseHexString(_hexTextBox.Text ?? string.Empty, HexInputAlphaPosition);

                if (convertedColor is Color color)
                {
                    SetCurrentValue(ColorProperty, color);
                }

                // Re-apply the hex value
                // This ensure the hex color value is always valid and formatted correctly
                SetColorToHexTextBox();
            }
        }

        /// <summary>
        /// Sets the current <see cref="Color"/> to the hex TextBox.
        /// </summary>
        private void SetColorToHexTextBox()
        {
            if (_hexTextBox != null)
            {
                _hexTextBox.Text = ColorToHexConverter.ToHexString(
                    Color,
                    HexInputAlphaPosition,
                    includeAlpha: (IsAlphaEnabled && IsAlphaVisible),
                    includeSymbol: false);
            }
        }

        /// <summary>
        /// This method is obsolete now, since the necessary validation is handled by the TabControl.
        /// It will be removed in a future release of Avalonia.
        /// </summary>
        // TODO-13: Remove this unused method 
        [Obsolete]
        protected virtual void ValidateSelection()
        {
            // Empty body for compatibility
        }
        
        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_hexTextBox != null)
            {
                _hexTextBox.KeyDown -= HexTextBox_KeyDown;
                _hexTextBox.LostFocus -= HexTextBox_LostFocus;
            }

            _hexTextBox = e.NameScope.Find<TextBox>("PART_HexTextBox");

            SetColorToHexTextBox();

            if (_hexTextBox != null)
            {
                _hexTextBox.KeyDown += HexTextBox_KeyDown;
                _hexTextBox.LostFocus += HexTextBox_LostFocus;
            }

            base.OnApplyTemplate(e);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (_ignorePropertyChanged)
            {
                base.OnPropertyChanged(change);
                return;
            }

            // Always keep the two color properties in sync
            if (change.Property == ColorProperty)
            {
                _ignorePropertyChanged = true;

                SetCurrentValue(HsvColorProperty, Color.ToHsv());
                SetColorToHexTextBox();

                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<Color>(),
                    change.GetNewValue<Color>()));

                _ignorePropertyChanged = false;
            }
            else if (change.Property == HsvColorProperty)
            {
                _ignorePropertyChanged = true;

                SetCurrentValue(ColorProperty, HsvColor.ToRgb());
                SetColorToHexTextBox();

                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<HsvColor>().ToRgb(),
                    change.GetNewValue<HsvColor>().ToRgb()));

                _ignorePropertyChanged = false;
            }
            else if (change.Property == PaletteProperty)
            {
                IColorPalette? palette = Palette;

                // Any custom palette change must be automatically synced with the
                // bound properties controlling the palette grid
                if (palette != null)
                {
                    SetCurrentValue(PaletteColumnCountProperty, palette.ColorCount);

                    List<Color> newPaletteColors = new List<Color>();
                    for (int shadeIndex = 0; shadeIndex < palette.ShadeCount; shadeIndex++)
                    {
                        for (int colorIndex = 0; colorIndex < palette.ColorCount; colorIndex++)
                        {
                            newPaletteColors.Add(palette.GetColor(colorIndex, shadeIndex));
                        }
                    }

                    SetCurrentValue(PaletteColorsProperty, newPaletteColors);
                }
            }
            else if (change.Property == IsAlphaEnabledProperty)
            {
                // Manually coerce the HsvColor value
                // (Color will be coerced automatically if HsvColor changes)
                SetCurrentValue(HsvColorProperty,  OnCoerceHsvColor(HsvColor));
            }

            base.OnPropertyChanged(change);
        }

        /// <summary>
        /// Raises the <see cref="ColorChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="ColorChangedEventArgs"/> defining old/new colors.</param>
        protected virtual void OnColorChanged(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Called when the <see cref="Color"/> property has to be coerced.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        protected virtual Color OnCoerceColor(Color value)
        {
            if (IsAlphaEnabled == false)
            {
                return new Color(255, value.R, value.G, value.B);
            }

            return value;
        }

        /// <summary>
        /// Called when the <see cref="HsvColor"/> property has to be coerced.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        protected virtual HsvColor OnCoerceHsvColor(HsvColor value)
        {
            if (IsAlphaEnabled == false)
            {
                return new HsvColor(1.0, value.H, value.S, value.V);
            }

            return value;
        }

        /// <summary>
        /// Coerces/validates the <see cref="Color"/> property value.
        /// </summary>
        /// <param name="instance">The <see cref="ColorView"/> instance.</param>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The coerced/validated value.</returns>
        private static Color CoerceColor(AvaloniaObject instance, Color value)
        {
            if (instance is ColorView colorView)
            {
                return colorView.OnCoerceColor(value);
            }

            return value;
        }

        /// <summary>
        /// Coerces/validates the <see cref="HsvColor"/> property value.
        /// </summary>
        /// <param name="instance">The <see cref="ColorView"/> instance.</param>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The coerced/validated value.</returns>
        private static HsvColor CoerceHsvColor(AvaloniaObject instance, HsvColor value)
        {
            if (instance is ColorView colorView)
            {
                return colorView.OnCoerceHsvColor(value);
            }

            return value;
        }

        /// <summary>
        /// Event handler for when a key is pressed within the Hex RGB value TextBox.
        /// This is used to trigger re-evaluation of the color based on the TextBox value.
        /// </summary>
        private void HexTextBox_KeyDown(object? sender, Input.KeyEventArgs e)
        {
            if (e.Key == Input.Key.Enter)
            {
                GetColorFromHexTextBox();
            }
        }

        /// <summary>
        /// Event handler for when the Hex RGB value TextBox looses focus.
        /// This is used to trigger re-evaluation of the color based on the TextBox value.
        /// </summary>
        private void HexTextBox_LostFocus(object? sender, Interactivity.RoutedEventArgs e)
        {
            GetColorFromHexTextBox();
        }
    }
}
