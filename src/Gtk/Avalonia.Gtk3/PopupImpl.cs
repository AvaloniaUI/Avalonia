using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class PopupImpl : TopLevelImpl, IPopupImpl
    {
        static GtkWindow CreateWindow()
        {
            var window = Native.GtkWindowNew(GtkWindowType.Popup);
            return window;
        }

        public PopupImpl() : base(CreateWindow())
        {


        }

        private Size _desiredSize = new Size(1, 1);
        public override Size ClientSize
        {
            get { return _desiredSize; }
            set
            {
                _desiredSize = value;
                if (GtkWidget.IsClosed)
                    return;
                Native.GtkWindowResize(GtkWidget, (int) value.Width, (int) value.Height);
                if (Native.GtkWidgetGetWindow(GtkWidget) == IntPtr.Zero)
                    Native.GtkWidgetRealize(GtkWidget);
                Native.GdkWindowResize(Native.GtkWidgetGetWindow(GtkWidget), (int) value.Width, (int) value.Height);
            }
        }
    }
}
