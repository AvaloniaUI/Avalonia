// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for controls that can contain multiple children.
    /// </summary>
    public interface IPanel : IControl
    {
        /// <summary>
        /// Gets the children of the <see cref="Panel"/>.
        /// </summary>
        Controls Children { get; }
    }
}