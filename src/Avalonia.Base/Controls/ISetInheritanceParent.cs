#nullable enable

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an interface through which a <see cref="StyledElement"/>'s inheritance parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// Additionally, <see cref="ISetLogicalParent"/> also sets the inheritance parent; this
    /// interface is only needed where the logical and inheritance parents differ.
    /// </remarks>
    public interface ISetInheritanceParent
    {
        /// <summary>
        /// Clears any explicit inheritance parent set via <see cref="SetParent(StyledElement?)"/>
        /// and reverts to the default value.
        /// </summary>
        void ClearParent();

        /// <summary>
        /// Sets the control's inheritance parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(StyledElement parent);
    }
}
