using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders.
    /// </summary>
    public partial class ColorView : TemplatedControl
    {
        /// <summary>
        /// Event for when the selected color changes within the slider.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        private bool disableUpdates = false;

        private ObservableCollection<Color> _customPaletteColors = new ObservableCollection<Color>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorView"/> class.
        /// </summary>
        public ColorView() : base()
        {
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            this.CustomPalette = new FluentColorPalette();

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

                OnColorChanged(new ColorChangedEventArgs(
                    change.GetOldValue<Color>(),
                    change.GetNewValue<Color>()));

                disableUpdates = false;
            }
            else if (change.Property == HsvColorProperty)
            {
                disableUpdates = true;

                Color = HsvColor.ToRgb();

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
    }
}
