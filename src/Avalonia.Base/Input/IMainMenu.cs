using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for a window's main menu.
    /// </summary>
    [NotClientImplementable]
    public interface IMainMenu
    {
        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        void Close();

        /// <summary>
        /// Opens the menu in response to the Alt/F10 key.
        /// </summary>
        void Open();

        /// <summary>
        /// Occurs when the main menu closes.
        /// </summary>
        event EventHandler<RoutedEventArgs>? MenuClosed;
    }
}
