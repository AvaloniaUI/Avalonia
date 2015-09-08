





namespace Perspex
{
    using Perspex.Collections;

    /// <summary>
    /// Represents a node in the logical tree.
    /// </summary>
    public interface ILogical
    {
        /// <summary>
        /// Gets the logical parent.
        /// </summary>
        ILogical LogicalParent { get; }

        /// <summary>
        /// Gets the logical children.
        /// </summary>
        IPerspexReadOnlyList<ILogical> LogicalChildren { get;  }
    }
}
