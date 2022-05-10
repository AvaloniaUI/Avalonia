#if IOS
using Avalonia.Platform;
using CoreGraphics;
using Foundation;
using UIKit;
using WebKit;
using Avalonia.iOS;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private IPlatformHandle CreateIOS(IPlatformHandle parent)
    {
        if (IsSecond)
        {
            var webView = new WKWebView(CGRect.Empty, new WKWebViewConfiguration());
            webView.LoadRequest(new NSUrlRequest(new NSUrl("https://www.apple.com/")));

            return new UIViewControlHandle(webView);
        }
        else
        {
            var button = new UIButton();
            var clickCount = 0;
            button.SetTitle("Hello world", UIControlState.Normal);
            button.BackgroundColor = UIColor.Blue;
            button.AddTarget((_, _) =>
            {
                clickCount++;
                button.SetTitle($"Click count {clickCount}", UIControlState.Normal);
            }, UIControlEvent.TouchDown);

            return new UIViewControlHandle(button);
        }
    }

    private void DestroyIOS(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}
#endif
