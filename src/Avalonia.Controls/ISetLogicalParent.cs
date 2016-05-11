// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an interface through which a <see cref="Control"/>'s logical parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// </remarks>
    public interface ISetLogicalParent
    {
        /// <summary>
        /// Sets the control's parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(ILogical parent);
    }
}