using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class PopupImpl : WindowBaseImpl, IPopupImpl
    {
        public PopupImpl()
        {
            UpdateStyle();
        }

        protected override NSWindowStyle GetStyle()
        {
            return NSWindowStyle.Borderless;
        }
    }
}