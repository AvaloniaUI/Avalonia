using System;
using Avalonia.Automation.Peers;
using Avalonia.Platform;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support UI Automation client access to the root of an
    /// automation tree.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by the <see cref="AutomationPeer"/> class, and should only
    /// be implemented on true root elements, such as Windows. To embed an automation tree, use
    /// <see cref="IEmbeddedRootProvider"/> instead.
    /// </remarks>
    public interface IRootProvider
    {
        /// <summary>
        /// Gets the platform implementation of the TopLevel for the element.
        /// </summary>
        ITopLevelImpl? PlatformImpl { get; }

        /// <summary>
        /// Gets the currently focused element.
        /// </summary>
        AutomationPeer? GetFocus();

        /// <summary>
        /// Gets the element at the specified point, expressed in top-level coordinates.
        /// </summary>
        /// <param name="p">The point.</param>
        AutomationPeer? GetPeerFromPoint(Point p);

        /// <summary>
        /// Raised by the automation peer when the focus changes.
        /// </summary>
        event EventHandler? FocusChanged;
    }
}
