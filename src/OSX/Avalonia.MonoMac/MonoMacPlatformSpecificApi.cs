using Avalonia.Controls.Platform;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    public class MonoMacPlatformSpecificApi : BaseOSXPlatformSpecificApiImpl
    {
        public override void ShowInDock(bool show)
        {
            NSApplication.SharedApplication.ActivationPolicy = show ? NSApplicationActivationPolicy.Regular : NSApplicationActivationPolicy.Accessory;
        }
    }
}