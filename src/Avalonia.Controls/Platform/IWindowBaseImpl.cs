using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IWindowBaseImpl : ITopLevelImpl
    {
        /// <summary>
        /// Gets the total size of the toplevel, excluding shadows.
        /// </summary>
        Size? FrameSize { get; }
        
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
        /// Gets the position of the window in device pixels.
        /// </summary>
        PixelPoint Position { get; }
        
        /// <summary>
        /// Gets or sets a method called when the window's position changes.
        /// </summary>
        Action<PixelPoint>? PositionChanged { get; set; }

        /// <summary>
        /// Activates the window.
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets or sets a method called when the window is deactivated (loses focus).
        /// </summary>
        Action? Deactivated { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is activated (receives focus).
        /// </summary>
        Action? Activated { get; set; }
       
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
