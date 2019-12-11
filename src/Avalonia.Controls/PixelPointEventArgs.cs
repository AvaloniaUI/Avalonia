// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides <see cref="PixelPoint"/> data for events.
    /// </summary>
    public class PixelPointEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PixelPointEventArgs"/> class.
        /// </summary>
        /// <param name="point">The <see cref="PixelPoint"/> data.</param>
        public PixelPointEventArgs(PixelPoint point)
        {
            Point = point;
        }

        /// <summary>
        /// Gets the <see cref="PixelPoint"/> data.
        /// </summary>
        public PixelPoint Point { get; }
    }
}
