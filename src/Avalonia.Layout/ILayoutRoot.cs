// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    public interface ILayoutRoot : ILayoutable
    {
        /// <summary>
        /// The size available to lay out the controls.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// The maximum client size available.
        /// </summary>
        Size MaxClientSize { get; }

        /// <summary>
        /// The scaling factor to use in layout.
        /// </summary>
        double LayoutScaling { get; }
    }
}
