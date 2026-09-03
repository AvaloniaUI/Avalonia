using System;
using Avalonia.X11.Dispatching;

namespace Avalonia.Wayland;

/// <summary>
/// GLib (GMainLoop) based UI-thread dispatcher for the Wayland backend, enabled via
/// <see cref="WaylandPlatformOptions.UseGLibMainLoop"/>. It lets Avalonia share a GLib main loop with GLib/GTK
/// based libraries on the UI thread. Unlike X11 it attaches no platform event source of its own: the Wayland
/// connection is owned and pumped by the worker thread, which posts input/events back via the dispatcher, so the
/// base class' signaling/timer/background machinery is all the UI thread needs.
/// </summary>
internal sealed class WaylandGlibDispatcher : GlibDispatcherImplBase
{
    public WaylandGlibDispatcher(Action<Exception>? externalExceptionLogger)
        : base(externalExceptionLogger)
    {
    }
}
