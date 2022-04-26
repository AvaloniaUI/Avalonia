using System;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A slider with a background that represents a single color component.
    /// </summary>
    public partial class ColorSlider : Slider
    {
        private Size cachedSize = Size.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorSlider"/> class.
        /// </summary>
        public ColorSlider() : base()
        {
        }

        /// <summary>
        /// Update the slider's Foreground and Background brushes based on the current slider state and color.
        /// </summary>
        /// <remarks>
        /// Manually refreshes the background gradient of the slider.
        /// This is callable separately for performance reasons.
        /// </remarks>
        public void UpdateColors()
        {
            HsvColor hsvColor = HsvColor;

            // Calculate and set the background
            UpdateBackground(hsvColor);

            // Calculate and set the foreground ensuring contrast with the background
            Color rgbColor = hsvColor.ToRgb();
            Color selectedRgbColor;
            double sliderPercent = Value / (Maximum - Minimum);

            var component = ColorComponent;

            if (ColorModel == ColorModel.Hsva)
            {
                if (IsAlphaMaxForced &&
                    component != ColorComponent.Alpha)
                {
                    hsvColor = new HsvColor(1.0, hsvColor.H, hsvColor.S, hsvColor.V);
                }

                switch (component)
                {
                    case ColorComponent.Component1:
                        {
                            var componentValue = MathUtilities.Clamp(sliderPercent * 360.0, 0.0, 360.0);

                            hsvColor = new HsvColor(
                                hsvColor.A,
                                componentValue,
                                IsSaturationValueMaxForced ? 1.0 : hsvColor.S,
                                IsSaturationValueMaxForced ? 1.0 : hsvColor.V);

                            break;
                        }

                    case ColorComponent.Component2:
                        {
                            var componentValue = MathUtilities.Clamp(sliderPercent * 1.0, 0.0, 1.0);

                            hsvColor = new HsvColor(
                                hsvColor.A,
                                hsvColor.H,
                                componentValue,
                                IsSaturationValueMaxForced ? 1.0 : hsvColor.V);

                            break;
                        }

                    case ColorComponent.Component3:
                        {
                            var componentValue = MathUtilities.Clamp(sliderPercent * 1.0, 0.0, 1.0);

                            hsvColor = new HsvColor(
                                hsvColor.A,
                                hsvColor.H,
                                IsSaturationValueMaxForced ? 1.0 : hsvColor.S,
                                componentValue);

                            break;
                        }
                }

                selectedRgbColor = hsvColor.ToRgb();
            }
            else
            {
                if (IsAlphaMaxForced &&
                    component != ColorComponent.Alpha)
                {
                    rgbColor = new Color(255, rgbColor.R, rgbColor.G, rgbColor.B);
                }

                byte componentValue = Convert.ToByte(MathUtilities.Clamp(sliderPercent * 255, 0, 255));

                switch (component)
                {
                    case ColorComponent.Component1:
                        rgbColor = new Color(rgbColor.A, componentValue, rgbColor.G, rgbColor.B);
                        break;
                    case ColorComponent.Component2:
                        rgbColor = new Color(rgbColor.A, rgbColor.R, componentValue, rgbColor.B);
                        break;
                    case ColorComponent.Component3:
                        rgbColor = new Color(rgbColor.A, rgbColor.R, rgbColor.G, componentValue);
                        break;
                }

                selectedRgbColor = rgbColor;
            }

            //var converter = new ContrastBrushConverter();
            //this.Foreground = converter.Convert(selectedRgbColor, typeof(Brush), this.DefaultForeground, null) as Brush;

            return;
        }

        /// <summary>
        /// Generates a new background image for the color slider and applies it.
        /// </summary>
        private async void UpdateBackground(HsvColor color)
        {
            // Updates may be requested when sliders are not in the visual tree.
            // For first-time load this is handled by the Loaded event.
            // However, after that problems may arise, consider the following case:
            //
            //   (1) Backgrounds are drawn normally the first time on Loaded.
            //       Actual height/width are available.
            //   (2) The palette tab is selected which has no sliders
            //   (3) The picker flyout is closed
            //   (4) Externally the color is changed
            //       The color change will trigger slider background updates but
            //       with the flyout closed, actual height/width are zero.
            //       No zero size bitmap can be generated.
            //   (5) The picker flyout is re-opened by the user and the default
            //       last-opened tab will be viewed: palette.
            //       No loaded events will be fired for sliders. The color change
            //       event was already handled in (4). The sliders will never
            //       be updated.
            //
            // In this case the sliders become out of sync with the Color because there is no way
            // to tell when they actually come into view. To work around this, force a re-render of
            // the background with the last size of the slider. This last size will be when it was
            // last loaded or updated.
            //
            // In the future additional consideration may be required for SizeChanged of the control.
            // This work-around will also cause issues if display scaling changes in the special
            // case where cached sizes are required.

            var width = Convert.ToInt32(Bounds.Width);
            var height = Convert.ToInt32(Bounds.Height);

            if (width == 0 || height == 0)
            {
                // Attempt to use the last size if it was available
                if (cachedSize.IsDefault == false)
                {
                    width = Convert.ToInt32(cachedSize.Width);
                    height = Convert.ToInt32(cachedSize.Height);
                }
            }
            else
            {
                cachedSize = new Size(width, height);
            }

            var bitmap = await ColorHelpers.CreateComponentBitmapAsync(
                width,
                height,
                Orientation,
                ColorModel,
                ColorComponent,
                color,
                IsAlphaMaxForced,
                IsSaturationValueMaxForced);

            if (bitmap != null)
            {
                Background = ColorHelpers.BitmapToBrushAsync(bitmap, width, height);
            }

            return;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            bool update = false;

            if (change.Property == ColorProperty)
            {
                // Sync with HSV (which is primary)
                HsvColor = Color.ToHsv();
                update = true;
            }
            else if (change.Property == HsvColorProperty)
            {
                update = true;
            }
            else if (change.Property == BoundsProperty)
            {
                update = true;
            }

            if (update && IsAutoUpdatingEnabled)
            {
                UpdateColors();
            }

            base.OnPropertyChanged(change);
        }
    }
}
