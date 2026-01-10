using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.Browser
{
    internal class BrowserInsetsManager : InsetsManagerBase
    {
        public override bool? IsSystemBarVisible
        {
            get
            {
                return DomHelper.IsFullscreen(BrowserWindowingPlatform.GlobalThis);
            }
            set
            {
                _ = DomHelper.SetFullscreen(BrowserWindowingPlatform.GlobalThis, !value ?? false);
            }
        }

        public override bool DisplayEdgeToEdgePreference { get; set; }

        public override Thickness SafeAreaPadding
        {
            get
            {
                var padding = DomHelper.GetSafeAreaPadding(BrowserWindowingPlatform.GlobalThis);

                return new Thickness(padding[0], padding[1], padding[2], padding[3]);
            }
        }

        public override Color? SystemBarColor { get; set; }

        public void NotifySafeAreaPaddingChanged()
        {
            OnSafeAreaChanged(new SafeAreaChangedArgs(SafeAreaPadding));
        }
    }
}
