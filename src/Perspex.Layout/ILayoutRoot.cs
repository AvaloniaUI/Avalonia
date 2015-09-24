// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    public interface ILayoutRoot : ILayoutable
    {
        /// <summary>
        /// The size available to layout the controls.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// The layout manager to use for laying out the tree.
        /// </summary>
        ILayoutManager LayoutManager { get; }
    }
}
