using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Interface for controls that present a single item of data inside a
    /// <see cref="TemplatedControl"/> template.
    /// </summary>
    [NotClientImplementable]
    public interface IContentPresenter : IPresenter
    {
        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        IControl? Child { get; }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        object? Content { get; set; }
    }
}
