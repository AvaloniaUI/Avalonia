using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel that can be used to virtualize items in an <see cref="ItemsControl"/>
    /// </summary>
    public interface IVirtualizingPanel : IPanel
    {
        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        IEnumerable<IControl> RealizedElements { get; }
    }
}
