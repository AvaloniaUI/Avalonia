using Avalonia.LogicalTree;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which hosts a content presenter.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="ContentControl"/> which usually contains a
    /// <see cref="ContentPresenter"/> and exposes it through its 
    /// <see cref="ContentControl.Presenter"/> property. ContentPresenters can be within
    /// nested templates or in popups and so are not necessarily created immediately when the
    /// parent control's template is instantiated so they register themselves using this 
    /// interface.
    /// </remarks>
    public interface IContentPresenterHost : ITemplatedControl
    {
        /// <summary>
        /// Called by the content presenter to register a logical child with the host.
        /// </summary>
        void RegisterLogicalChild(IContentPresenter presenter, ILogical? child);

        /// <summary>
        /// Registers an <see cref="IContentPresenter"/> with a host control.
        /// </summary>
        /// <param name="presenter">The content presenter.</param>
        void RegisterContentPresenter(IContentPresenter presenter);
    }
}
