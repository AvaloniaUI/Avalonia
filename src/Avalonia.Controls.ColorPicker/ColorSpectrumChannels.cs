// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the two HSV color channels displayed by a <see cref="ColorSpectrum"/>.
    /// </summary>
    /// <remarks>
    /// Order of the color channels is important and correspond with an X/Y axis in Box
    /// shape or a degree/radius in Ring shape.
    /// </remarks>
    public enum ColorSpectrumChannels
    {
        /// <summary>
        /// The Hue and Value channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Hue is mapped to the X-axis and Value is mapped to the Y-axis.
        /// In Ring shape, Hue is mapped to degrees and Value is mapped to radius.
        /// </remarks>
        HueValue,

        /// <summary>
        /// The Value and Hue channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Value is mapped to the X-axis and Hue is mapped to the Y-axis.
        /// In Ring shape, Value is mapped to degrees and Hue is mapped to radius.
        /// </remarks>
        ValueHue,

        /// <summary>
        /// The Hue and Saturation channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Hue is mapped to the X-axis and Saturation is mapped to the Y-axis.
        /// In Ring shape, Hue is mapped to degrees and Saturation is mapped to radius.
        /// </remarks>
        HueSaturation,

        /// <summary>
        /// The Saturation and Hue channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Saturation is mapped to the X-axis and Hue is mapped to the Y-axis.
        /// In Ring shape, Saturation is mapped to degrees and Hue is mapped to radius.
        /// </remarks>
        SaturationHue,

        /// <summary>
        /// The Saturation and Value channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Saturation is mapped to the X-axis and Value is mapped to the Y-axis.
        /// In Ring shape, Saturation is mapped to degrees and Value is mapped to radius.
        /// </remarks>
        SaturationValue,

        /// <summary>
        /// The Value and Saturation channels.
        /// </summary>
        /// <remarks>
        /// In Box shape, Value is mapped to the X-axis and Saturation is mapped to the Y-axis.
        /// In Ring shape, Value is mapped to degrees and Saturation is mapped to radius.
        /// </remarks>
        ValueSaturation,
    };
}
