using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents a layout root that is embedded somwhere that dictates its measure constraint.
    /// </summary>
    public interface IEmbeddedLayoutRoot : ILayoutRoot
    {
        /// <summary>
        /// Gets the constraint with which the root should be measured.
        /// </summary>
        /// <remarks>
        /// By default a layout root is measured with Size.Infinity indicating that it can try to
        /// grow as much as it wishes. On the other hand, if a layout root is embedded in another
        /// UI framework then it will have a constraint dictated by that framework - if that is the
        /// case then top level control should implement this interface and return the parent
        /// constraint here.
        /// </remarks>
        Size EmbeddedConstraint { get; }
    }
}
