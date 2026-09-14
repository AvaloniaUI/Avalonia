using System;
using static Avalonia.Wayland.Server.Interop.XkbCommonNativeMethods;

namespace Avalonia.Wayland;

/// <summary>
/// Wraps an xkb_compose_table loaded from the system locale.
/// </summary>
sealed class XkbComposeTable : IDisposable
{
    public IntPtr Handle { get; private set; }

    private XkbComposeTable(IntPtr handle)
    {
        Handle = handle;
    }

    /// <summary>
    /// Creates a compose table from the system locale, or returns null if unavailable.
    /// </summary>
    public static XkbComposeTable? TryCreate(XkbContext context)
    {
        var locale = Environment.GetEnvironmentVariable("LC_ALL")
                     ?? Environment.GetEnvironmentVariable("LC_CTYPE")
                     ?? Environment.GetEnvironmentVariable("LANG")
                     ?? "C";

        var handle = xkb_compose_table_new_from_locale(
            context.Handle, locale, XKB_COMPOSE_COMPILE_NO_FLAGS);

        return handle == IntPtr.Zero ? null : new XkbComposeTable(handle);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            xkb_compose_table_unref(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
