using System;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop;

/// <summary>
/// Backend-provided lease on a <c>parent_window</c> handle string suitable for passing
/// to <c>org.freedesktop.portal.FileChooser</c> et al.
/// </summary>
/// <remarks>
/// <para>The handle string is in the platform-prefixed format defined by xdg-desktop-portal:
/// <c>x11:HEX</c> for X11, <c>wayland:HANDLE</c> for wayland.</para>
/// <para>Disposal frees any platform resources backing the handle. For wayland this destroys
/// the <c>zxdg_exported_v2</c> object that provided the handle string; the imported handle
/// on the portal side becomes invalid afterwards, so the lease MUST be held until the
/// portal call completes.</para>
/// </remarks>
internal interface IPortalParentLease : IAsyncDisposable
{
    /// <summary>Prefixed parent-window handle string (e.g. <c>"x11:1A2B"</c>).</summary>
    string Handle { get; }
}

internal sealed class TrivialPortalParentLease : IPortalParentLease
{
    public TrivialPortalParentLease(string handle) => Handle = handle;
    public string Handle { get; }
    public ValueTask DisposeAsync() => default;
}
