// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the two HSV color channels displayed by a <see cref="ColorSpectrum"/>.
    /// Order of the color channels is important.
    /// </summary>
    public enum ColorSpectrumChannels
    {
        /// <summary>
        /// The Hue and Value channels.
        /// </summary>
        HueValue,

        /// <summary>
        /// The Value and Hue channels.
        /// </summary>
        ValueHue,

        /// <summary>
        /// The Hue and Saturation channels.
        /// </summary>
        HueSaturation,

        /// <summary>
        /// The Saturation and Hue channels.
        /// </summary>
        SaturationHue,

        /// <summary>
        /// The Saturation and Value channels.
        /// </summary>
        SaturationValue,

        /// <summary>
        /// The Value and Saturation channels.
        /// </summary>
        ValueSaturation,
    };
}
