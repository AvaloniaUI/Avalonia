using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Wayland.Screens;

namespace Avalonia.Wayland;

/// <summary>
/// Wayland-only window operations that have no cross-platform equivalent.
/// </summary>
public static class WaylandWindowExtensions
{
    /// <summary>
    /// Asks the compositor to put this window fullscreen on <paramref name="screen"/>
    /// rather than on a monitor of its own choosing, by naming the output in the
    /// <c>xdg_toplevel.set_fullscreen</c> request.
    ///
    /// Call it before <see cref="Window.Show()"/> to have the window mapped on that
    /// screen; calling it while the window is already fullscreen moves it there.
    /// Setting <see cref="Window.WindowState"/> to <see cref="WindowState.FullScreen"/>
    /// is still what turns fullscreen on — this only decides where.
    ///
    /// A null <paramref name="screen"/> restores the default (compositor picks).
    /// Returns false if this window is not a Wayland window, or if the screen did
    /// not come from the Wayland screen list, in which case nothing changes.
    /// </summary>
    public static bool TrySetFullscreenScreen(this Window window, Screen? screen)
    {
        if (window.PlatformImpl is not WindowImpl impl)
            return false;

        if (screen is null)
        {
            impl.SetFullscreenOutput(null);
            return true;
        }

        if (screen.TryGetPlatformHandle() is not WaylandScreenHandle handle)
            return false;

        impl.SetFullscreenOutput(handle.Id);
        return true;
    }
}
