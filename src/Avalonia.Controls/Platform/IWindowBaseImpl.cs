using System;
using Avalonia.Controls;

namespace Avalonia.Platform
{
    public interface IWindowBaseImpl : ITopLevelImpl
    {
        /// <summary>
        /// Shows the top level.
        /// </summary>
        void Show();

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
        Action<PixelPoint> PositionChanged { get; set; }

        /// <summary>
        /// Activates the window.
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets or sets a method called when the window is deactivated (loses focus).
        /// </summary>
        Action Deactivated { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is activated (receives focus).
        /// </summary>
        Action Activated { get; set; }

        /// <summary>
        /// Gets the platform window handle.
        /// </summary>
        IPlatformHandle Handle { get; }
       
        /// <summary>
        /// Gets the maximum size of a window on the system.
        /// </summary>
        Size MaxClientSize { get; }

        /// <summary>
        /// Sets whether this window appears on top of all other windows
        /// </summary>
        void SetTopmost(bool value);

        /// <summary>
        /// Gets platform specific display information
        /// </summary>
        IScreenImpl Screen { get; }
    }
}
