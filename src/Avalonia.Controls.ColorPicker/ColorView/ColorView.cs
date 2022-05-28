using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private TextBox? _hexTextBox;

        private ObservableCollection<Color> _customPaletteColors = new ObservableCollection<Color>();
        private ColorToHexConverter colorToHexConverter = new ColorToHexConverter();
        private bool disableUpdates = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorView"/> class.
        /// </summary>
        public ColorView() : base()
        {
            this.CustomPalette = new FluentColorPalette();
        }

        /// <summary>
        /// Gets the value of the hex TextBox and sets it as the current <see cref="Color"/>.
        /// If invalid, the TextBox hex text will revert back to the last valid color.
        /// </summary>
        private void GetColorFromHexTextBox()
        {
            if (_hexTextBox != null)
            {
                var convertedColor = colorToHexConverter.ConvertBack(_hexTextBox.Text, typeof(Color), null, CultureInfo.CurrentCulture);

                if (convertedColor is Color color)
                {
                    Color = color;
                }

                // Re-apply the hex value
                // This ensure the hex color value is always valid and formatted correctly
                _hexTextBox.Text = colorToHexConverter.Convert(Color, typeof(string), null, CultureInfo.CurrentCulture) as string;
            }
        }

        /// <summary>
        /// Sets the current <see cref="Color"/> to the hex TextBox.
        /// </summary>
        private void SetColorToHexTextBox()
        {
            if (_hexTextBox != null)
            {
                _hexTextBox.Text = colorToHexConverter.Convert(Color, typeof(string), null, CultureInfo.CurrentCulture) as string;
            }
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
            if (disableUpdates)
            {
                base.OnPropertyChanged(change);
                return;
            }

            // Always keep the two color properties in sync
            if (change.Property == ColorProperty)
            {
                disableUpdates = true;

                HsvColor = Color.ToHsv();
                SetColorToHexTextBox();

                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<Color>(),
                    change.GetNewValue<Color>()));

                disableUpdates = false;
            }
            else if (change.Property == HsvColorProperty)
            {
                disableUpdates = true;

                Color = HsvColor.ToRgb();
                SetColorToHexTextBox();

                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<HsvColor>().ToRgb(),
                    change.GetNewValue<HsvColor>().ToRgb()));

                disableUpdates = false;
            }
            else if (change.Property == CustomPaletteProperty)
            {
                IColorPalette? palette = CustomPalette;

                // Any custom palette change must be automatically synced with the
                // bound properties controlling the palette grid
                if (palette != null)
                {
                    CustomPaletteColumnCount = palette.ColorCount;
                    CustomPaletteColors.Clear();

                    for (int shadeIndex = 0; shadeIndex < palette.ShadeCount; shadeIndex++)
                    {
                        for (int colorIndex = 0; colorIndex < palette.ColorCount; colorIndex++)
                        {
                            CustomPaletteColors.Add(palette.GetColor(colorIndex, shadeIndex));
                        }
                    }
                }
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
