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
        /// <param name="newPos">The new window position.</param>
        public PointEventArgs(Point newPos)
        {
            NewPosition = newPos;
        }

        /// <summary>
        /// Gets the new window position.
        /// </summary>
        public Point NewPosition { get; }
    }
}