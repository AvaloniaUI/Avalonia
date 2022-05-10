#if IOS
using System;
using System.IO;
using System.Diagnostics;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using CoreGraphics;
using Foundation;
using UIKit;
using WebKit;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private IPlatformHandle CreateIOS(IPlatformHandle parent)
    {
        if (IsSecond)
        {
            var webView = new WKWebView(CGRect.Empty, new WKWebViewConfiguration());
            webView.LoadRequest(new NSUrlRequest(new NSUrl("https://www.apple.com/")));

            return new UIViewHandle(webView);
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

            return new UIViewHandle(button);   
        }
    }

    private void DestroyIOS(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}

internal class UIViewHandle : INativeControlHostDestroyableControlHandle
{
    private UIView _view;

    public UIViewHandle(UIView view)
    {
        _view = view;
    }

    public IntPtr Handle => _view?.Handle ?? IntPtr.Zero;
    public string HandleDescriptor => "UIView";

    public void Destroy()
    {
        _view?.Dispose();
        _view = null;
    }
}
#endif
