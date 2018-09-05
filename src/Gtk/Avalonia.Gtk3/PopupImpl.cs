using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        static GtkWindow CreateWindow()
        {
            var window = Native.GtkWindowNew(GtkWindowType.Popup);
            return window;
        }

        public PopupImpl() : base(CreateWindow())
        {
            OverrideRedirect = true;
        }
    }
}
