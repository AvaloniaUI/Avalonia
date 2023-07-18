using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Avalonia.X11.Interop;

public class GtkInteropHelper
{
    public static async Task<T> RunOnGlibThread<T>(Func<T> cb)
    {
        if (!await NativeDialogs.Gtk.StartGtk().ConfigureAwait(false))
            throw new Win32Exception("Unable to initialize GTK");
        return await NativeDialogs.Glib.RunOnGlibThread(cb).ConfigureAwait(false);
    }
}