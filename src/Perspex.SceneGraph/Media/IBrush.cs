// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    public interface IBrush
    {
        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        double Opacity { get; }
    }
}