





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
