using System.Collections.Generic;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which presents the list of items inside the template of an
    /// <see cref="ItemsControl"/>.
    /// </summary>
    public interface IItemsPresenter : IPresenter
    {
        /// <summary>
        /// Gets the <see cref="IPanel"/> on which the item containers are hosted.
        /// </summary>
        IPanel Panel { get; }

        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        IEnumerable<IControl> RealizedElements { get; }
    }
}
