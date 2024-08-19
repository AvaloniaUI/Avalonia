using System;
using Avalonia.Platform;
using CoreGraphics;
using Foundation;
using UIKit;
using Avalonia.iOS;
using ControlCatalog.Pages;

namespace ControlCatalog;

public class EmbedSampleIOS : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
#if !TVOS
        if (isSecond)
        {
            var webView = new WebKit.WKWebView(CGRect.Empty, new WebKit.WKWebViewConfiguration());
            webView.LoadRequest(new NSUrlRequest(new NSUrl("https://www.apple.com/")));

            return new UIViewControlHandle(webView);
        }
        else
#endif
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
}
