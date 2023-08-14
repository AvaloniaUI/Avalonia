using System;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposure methods and properties to support UI Automation client access to the root of an
    /// automation tree hosted by another UI framework.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by the <see cref="AutomationPeer"/> class, and can be used
    /// to embed an automation tree from a 3rd party UI framework that wishes to use Avalonia's
    /// automation support.
    /// </remarks>
    public interface IEmbeddedRootProvider
    {
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
