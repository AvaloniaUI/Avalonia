





namespace Perspex.Controls.Presenters
{
    using Perspex.Controls.Primitives;

    /// <summary>
    /// Interface for controls that present a single item of data inside a
    /// <see cref="TemplatedControl"/> template.
    /// </summary>
    public interface IContentPresenter : IPresenter
    {
        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        IControl Child { get; }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        object Content { get; set; }
    }
}