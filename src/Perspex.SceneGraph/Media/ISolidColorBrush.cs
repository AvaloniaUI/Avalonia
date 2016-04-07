// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public interface ISolidColorBrush : IBrush
    {
        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        Color Color { get; }
    }
}