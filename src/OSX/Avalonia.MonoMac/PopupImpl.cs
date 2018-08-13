using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        public PopupImpl()
        {
            UpdateStyle();
            Window.Level = NSWindowLevel.PopUpMenu;
        }

        protected override NSWindowStyle GetStyle()
        {
            return NSWindowStyle.Borderless;
        }

        protected override CustomWindow CreateCustomWindow() => new CustomPopupWindow(this);

        private class CustomPopupWindow : CustomWindow
        {
            public CustomPopupWindow(WindowBaseImpl impl)
                : base(impl)
            { }

            public override bool WorksWhenModal() => true;
        }
    }
}
