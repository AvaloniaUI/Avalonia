// Portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using System;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds the details of a ColorChanged event.
    /// </summary>
    /// <remarks>
    /// HSV color information is intentionally not provided.
    /// Use <see cref="Color.ToHsv()"/> to obtain it.
    /// </remarks>
    public class ColorChangedEventArgs : EventArgs
    {
        private Color _OldColor;
        private Color _NewColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorChangedEventArgs"/> class.
        /// </summary>
        public ColorChangedEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldColor">The old/original color from before the change event.</param>
        /// <param name="newColor">The new/updated color that triggered the change event.</param>
        public ColorChangedEventArgs(Color oldColor, Color newColor)
        {
            _OldColor = oldColor;
            _NewColor = newColor;
        }

        /// <summary>
        /// Gets the old/original color from before the change event.
        /// </summary>
        public Color OldColor
        {
            get => _OldColor;
            internal set => _OldColor = value;
        }

        /// <summary>
        /// Gets the new/updated color that triggered the change event.
        /// </summary>
        public Color NewColor
        {
            get => _NewColor;
            internal set => _NewColor = value;
        }
    }
}
