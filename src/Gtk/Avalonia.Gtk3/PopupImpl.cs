using System;
using System.Collections.Generic;
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
            Native.GtkWindowSetSizeRequest(window, 1, 1);
            Native.GtkWindowSetDefaultSize(window, 200, 200);

            return window;
        }

        public PopupImpl() : base(CreateWindow())
        {


        }

        public override Size ClientSize
        {
            get
            {
                return base.ClientSize;
            }
            set
            {
                if(GtkWidget.IsClosed)
                    return;
                Native.GtkWindowSetDefaultSize(GtkWidget, (int)value.Width, (int)value.Height);
                base.ClientSize = value;
                var size = ClientSize;
            }
        }
    }
}
