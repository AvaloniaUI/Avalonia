using Avalonia.Metadata;

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    [NotClientImplementable]
    public interface ILayoutRoot
    {
        /// <summary>
        /// The size available to lay out the controls.
        /// </summary>
        Size ClientSize { get; }

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
