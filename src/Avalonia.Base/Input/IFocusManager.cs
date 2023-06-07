using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    [NotClientImplementable]
    public interface IFocusManager
    {
        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        IInputElement? GetFocusedElement();

        /// <summary>
        /// Clears currently focused element.
        /// </summary>
        [Unstable("This API might be removed in 11.x minor updates. Please consider focusing another element instead of removing focus at all for better UX.")]
        void ClearFocus();
    }
}
