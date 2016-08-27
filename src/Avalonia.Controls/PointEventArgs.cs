// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides <see cref="Point"/> data for events.
    /// </summary>
    public class PointEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointEventArgs"/> class.
        /// </summary>
        /// <param name="data">The <see cref="Point"/> data.</param>
        public PointEventArgs(Point data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the <see cref="Point"/> data.
        /// </summary>
        public Point Data { get; }
    }
}