using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Gtk
{
    using System;
    using Perspex.Platform;
    using global::Gtk;

    public class PopupImpl : WindowImpl, IPopupImpl
    {
        public PopupImpl()
            : base(WindowType.Popup)
        {
            this.DefaultSize = new Gdk.Size(640, 480);
            this.Events = Gdk.EventMask.PointerMotionMask |
                          Gdk.EventMask.ButtonPressMask |
                          Gdk.EventMask.ButtonReleaseMask;
        }

        public void SetPosition(Point p)
        {
            this.Move((int)p.X, (int)p.Y);
        }
    }
}
