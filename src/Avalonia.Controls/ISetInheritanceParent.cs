// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an interface through which a <see cref="Control"/>'s inheritance parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// Additionally, <see cref="ISetLogicalParent"/> also sets the inheritance parent; this
    /// interface is only needed where the logical and inheritance parents differ.
    /// </remarks>
    public interface ISetInheritanceParent
    {
        /// <summary>
        /// Sets the control's inheritance parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(IAvaloniaObject parent);
    }
}