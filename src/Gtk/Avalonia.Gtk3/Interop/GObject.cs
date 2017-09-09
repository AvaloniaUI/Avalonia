using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Gtk3.Interop
{
    class GObject : SafeHandle
    {
        public GObject() : base(IntPtr.Zero, true)
        {
        }

        public GObject(IntPtr handle, bool owned = true) : base(IntPtr.Zero, owned)
        {
            this.handle = handle;
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
                Native.GObjectUnref(handle);
            handle = IntPtr.Zero;
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    class GInputStream : GObject
    {
    }

    class GtkWidget : GObject
    {
    }

    class GtkWindow : GtkWidget
    {
        public static GtkWindow Null { get; } = new GtkWindow();
    }
    
    class GtkScreen : GObject
    {
    }


    class GtkImContext : GObject
    {
    }

    class GtkDialog : GtkWindow
    {
    }

    class GtkFileChooser : GtkDialog
    {
    }
}