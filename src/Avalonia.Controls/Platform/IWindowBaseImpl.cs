using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IWindowBaseImpl : ITopLevelImpl
    {
        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <param name="activate">Whether to activate the shown window.</param>
        /// <param name="isDialog">Whether the window is being shown as a dialog.</param>
        void Show(bool activate, bool isDialog);

        /// <summary>
        /// Hides the window.
        /// </summary>
        void Hide();

        /// <summary>
        /// Activates the window.
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets a maximum client size hint for an auto-sizing window, in device-independent pixels.
        /// </summary>
        Size MaxAutoSizeHint { get; }

        /// <summary>
        /// Sets whether this window appears on top of all other windows
        /// </summary>
        void SetTopmost(bool value);
    }
}
