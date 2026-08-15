using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable, PrivateApi]
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();

        ITopLevelImpl CreateEmbeddableTopLevel();
        
        IWindowImpl CreateEmbeddableWindow();

        ITrayIconImpl? CreateTrayIcon();

        /// <summary>
        /// Fills a span with numbers that represent the relative order of the windows in the z-order.
        /// The topmost window should have the highest number.
        /// Both the <paramref name="windows"/> and <paramref name="zOrder"/> lists are expected to be the same length.
        /// </summary>
        /// <param name="windows">A span of windows to get their z-order.</param>
        /// <param name="zOrder">The span to be filled with the associated window z-order.</param>
        void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder);
    }
}
