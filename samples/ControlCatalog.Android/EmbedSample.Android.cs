using System;
using Avalonia.Platform;
using Avalonia.Android;
using ControlCatalog.Pages;

namespace ControlCatalog.Android;

public class EmbedSampleAndroid : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        var parentContext = (parent as AndroidViewControlHandle)?.View.Context
            ?? global::Android.App.Application.Context;

        if (isSecond)
        {
            var webView = new global::Android.Webkit.WebView(parentContext);
            webView.LoadUrl("https://www.android.com/");

            return new AndroidViewControlHandle(webView);
        }
        else
        {
            var button = new global::Android.Widget.Button(parentContext) { Text = "Hello world" };
            var clickCount = 0;
            button.Click += (sender, args) =>
            {
                clickCount++;
                button.Text = $"Click count {clickCount}";
            };

            return new AndroidViewControlHandle(button);
        }
    }
}
