#if __ANDROID__ || ANDROID
using Avalonia.Platform;
using Avalonia.Android;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private IPlatformHandle CreateAndroid(IPlatformHandle parent)
    {
        var parentContext = (parent as AndroidViewControlHandle)?.View.Context
            ?? Android.App.Application.Context;

        if (IsSecond)
        {
            var webView = new Android.Webkit.WebView(parentContext);
            webView.LoadUrl("https://www.android.com/");

            return new AndroidViewControlHandle(webView);
        }
        else
        {
            var button = new Android.Widget.Button(parentContext) { Text = "Hello world" };
            var clickCount = 0;
            button.Click += (sender, args) =>
            {
                clickCount++;
                button.Text = $"Click count {clickCount}";
            };

            return new AndroidViewControlHandle(button);
        }
    }

    private void DestroyAndroid(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}
#endif
