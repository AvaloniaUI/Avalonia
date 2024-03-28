using System;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable, PrivateApi]
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();

        /// <summary>
        /// Fills zOrder with numbers that represent the relative order of the windows in the z-order.
        /// The topmost window should have the highest number.
        /// Both the windows and zOrder lists are expected to be the same length.
        /// </summary>
        /// <param name="windows">A span of windows to get their z-order</param>
        /// <param name="zOrder">Span to be filled with associated window z-order</param>
        void GetWindowsZOrder(Span<Window> windows, Span<long> zOrder);
    }
}
