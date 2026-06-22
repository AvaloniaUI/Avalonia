using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.OpenGL;

// XxxPlatformOptions are deliberately in global Avalonia namespace
// ReSharper disable once CheckNamespace
namespace Avalonia;

/// <summary>
/// Platform-specific options for the Wayland windowing backend, supplied via
/// <see cref="AvaloniaWaylandPlatformExtensions.UseWayland"/>.
/// </summary>
public class WaylandPlatformOptions
{
    /// <summary>
    /// The name of the Wayland display to connect to (e.g. <c>wayland-0</c>). When <c>null</c>,
    /// the <c>WAYLAND_DISPLAY</c> environment variable is used. Ignored when <see cref="DisplayFd"/> is set.
    /// </summary>
    public string? WlDisplayName { get; set; }

    /// <summary>
    /// An already-opened file descriptor for the Wayland display socket.
    /// When set, <see cref="WlDisplayName"/> is ignored and
    /// <c>wl_display_connect_to_fd</c> is used instead of <c>wl_display_connect</c>.
    /// Reconnects are automatically disabled in this mode because the fd
    /// is consumed by <c>libwayland</c> and cannot be reused.
    /// </summary>
    public int? DisplayFd { get; set; }

    /// <summary>
    /// Whether to automatically attempt to reconnect to the compositor if the connection is lost.
    /// Defaults to enabled. Reconnects are always disabled when <see cref="DisplayFd"/> is set, because
    /// the file descriptor is consumed by <c>libwayland</c> and cannot be reused.
    /// </summary>
    public bool? EnableReconnects { get; set; }

    /// <summary>
    /// Suppresses server-side decoration negotiation
    /// (<c>zxdg_decoration_manager_v1</c>): toplevels behave as if the
    /// compositor never advertised SSD support. Used primarily for
    /// testing the CSD path on compositors that would otherwise enforce
    /// server-side decorations (KWin, etc.). Equivalent in intent to
    /// <c>X11PlatformOptions.ForceDrawnDecorations</c>.
    /// </summary>
    [Experimental("AVALONIA_WAYLAND_FORCE_CSD"
#if NET10_0_OR_GREATER
        , Message = "Experimental, used mostly for testing"
#endif
    )]
    public bool ForceDrawnDecorations { get; set; }

#pragma warning disable AVALONIA_WAYLAND_FORCE_CSD
    internal bool ForceDrawnDecorationsInternal => ForceDrawnDecorations;
#pragma warning restore AVALONIA_WAYLAND_FORCE_CSD

    /// <summary>
    /// The OpenGL/OpenGL ES versions to try, in priority order, when creating the GL context.
    /// The first profile the driver supports is used.
    /// </summary>
    public IList<GlVersion> GlProfiles { get; set; } = new List<GlVersion>
    {
        new GlVersion(GlProfileType.OpenGL, 4, 0),
        new GlVersion(GlProfileType.OpenGL, 3, 2),
        new GlVersion(GlProfileType.OpenGL, 3, 0),
        new GlVersion(GlProfileType.OpenGLES, 3, 2),
        new GlVersion(GlProfileType.OpenGLES, 3, 0),
        new GlVersion(GlProfileType.OpenGLES, 2, 0)
    };

    /// <summary>
    /// Whether to use a dmabuf-based swapchain for GPU rendering. When <c>null</c>, the backend
    /// decides based on compositor and driver capabilities.
    /// </summary>
    public bool? UseDmabufSwapchain { get; set; }
}