using System;

using Avalonia.Platform;
using Avalonia.Threading;

using ControlCatalog.Pages;

using MonoMac.Foundation;
using MonoMac.WebKit;

namespace ControlCatalog.NetCore;

public class EmbedSampleMac : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        // Note: We are using MonoMac for example purposes
        // It shouldn't be used in production apps
        MacHelper.EnsureInitialized();

        var webView = new WebView();
        Dispatcher.UIThread.Post(() =>
        {
            webView.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl(
                isSecond ? "https://bing.com" : "https://google.com/")));
        });
        return new MacOSViewHandle(webView);
    }
}
