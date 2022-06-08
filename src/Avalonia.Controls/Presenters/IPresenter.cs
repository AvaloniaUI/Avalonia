using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Interface for presenters such as <see cref="ContentPresenter"/> and
    /// <see cref="ItemsPresenter"/>.
    /// </summary>
    /// <remarks>
    /// A presenter is the gateway between a templated control and its content. When
    /// a control which implements <see cref="IPresenter"/> is found in the template
    /// of a <see cref="TemplatedControl"/> then that signals that the visual child
    /// of the presenter is not a part of the template.
    /// </remarks>
    [NotClientImplementable]
    public interface IPresenter : IControl, INamed
    {
    }
}
