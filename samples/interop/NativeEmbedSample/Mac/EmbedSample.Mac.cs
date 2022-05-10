#if DESKTOP
using Avalonia.Platform;
using Avalonia.Threading;
using MonoMac.Foundation;
using MonoMac.WebKit;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    IPlatformHandle CreateOSX(IPlatformHandle parent)
    {
        // Note: We are using MonoMac for example purposes
        // It shouldn't be used in production apps
        MacHelper.EnsureInitialized();

        var webView = new WebView();
        Dispatcher.UIThread.Post(() =>
        {
            webView.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl(
                IsSecond ? "https://bing.com": "https://google.com/")));
        });
        return new MacOSViewHandle(webView);

    }

    void DestroyOSX(IPlatformHandle handle)
    {
        ((MacOSViewHandle)handle).Dispose();
    }
}
#endif
