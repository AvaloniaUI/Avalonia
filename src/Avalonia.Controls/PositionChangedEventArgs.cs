// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="TopLevel.PositionChanged"/> event.
    /// </summary>
    public class PositionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="newPos">The new window position.</param>
        public PositionChangedEventArgs(Point newPos)
        {
            NewPosition = newPos;
        }

        /// <summary>
        /// Gets the new window position.
        /// </summary>
        public Point NewPosition { get; }
    }
}