using Avalonia.Metadata;

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    internal interface ILayoutRoot
    {
        /// <summary>
        /// The scaling factor to use in layout.
        /// </summary>
        double LayoutScaling { get; }

        /// <summary>
        /// Associated instance of layout manager
        /// </summary>
        internal ILayoutManager LayoutManager { get; }
    }
}
