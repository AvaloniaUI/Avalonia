using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Platform;
using Gdk;
using Gtk;

namespace Avalonia.Gtk.Embedding
{
    public class GtkAvaloniaControlHost : DrawingArea, IPlatformHandle
    {
        private EmbeddableControlRoot _root;

        public GtkAvaloniaControlHost()
        {
            _root = new EmbeddableControlRoot(new EmbeddableImpl(this));
            _root.Prepare();
        }

        private Control _content;

        public Control Content
        {
            get { return _content; }
            set
            {
                _content = value;
                if (_root != null)
                {
                    _root.Content = value;
                    _root.Prepare();
                }
            }
        }

        protected override void OnSizeRequested(ref Requisition requisition)
        {
            requisition.Width = 700;
            requisition.Height = 500;
           
        }


        IntPtr IPlatformHandle.Handle => PlatformHandleAwareWindow.GetNativeWindow(GdkWindow);

        string IPlatformHandle.HandleDescriptor => "HWND";
    }
}
