using System.Collections.Generic;
using Avalonia.Controls.Generators;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which presents the list of items inside the template of an
    /// <see cref="ItemsControl"/>.
    /// </summary>
    public interface IItemsPresenter : IPresenter
    {
        /// <summary>
        /// Gets the container generator for the presenter.
        /// </summary>
        IItemContainerGenerator? ItemContainerGenerator { get; }

        /// <summary>
        /// Gets an <see cref="ItemsSourceView"/> containing the items to display.
        /// </summary>
        ItemsSourceView? ItemsView { get; }

        /// <summary>
        /// Gets the <see cref="IPanel"/> on which the item containers are hosted.
        /// </summary>
        IPanel? Panel { get; }

        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        IEnumerable<IControl> RealizedElements { get; }
    }
}
