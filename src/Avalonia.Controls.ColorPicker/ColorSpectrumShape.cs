// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the shape of a <see cref="ColorSpectrum"/>.
    /// </summary>
    public enum ColorSpectrumShape
    {
        /// <summary>
        /// The spectrum is in the shape of a rectangular or square box.
        /// Note that more colors are visible to the user in Box shape.
        /// </summary>
        Box,

        /// <summary>
        /// The spectrum is in the shape of an ellipse or circle.
        /// </summary>
        Ring,
    };
}
