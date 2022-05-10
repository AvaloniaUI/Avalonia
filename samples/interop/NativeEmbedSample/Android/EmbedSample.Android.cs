#if __ANDROID__ || ANDROID
using System;
using System.IO;
using System.Diagnostics;
using Android.Views;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private IPlatformHandle CreateAndroid(IPlatformHandle parent)
    {
        if (IsSecond)
        {
            var webView = new Android.Webkit.WebView(Android.App.Application.Context);
            webView.LoadUrl("https://www.android.com/");

            return new AndroidViewHandle(webView);
        }
        else
        {
            var button = new Android.Widget.Button(Android.App.Application.Context) { Text = "Hello world" };
            var clickCount = 0;
            button.Click += (sender, args) =>
            {
                clickCount++;
                button.Text = $"Click count {clickCount}";
            };
            return new AndroidViewHandle(button);   
        }
    }

    private void DestroyAndroid(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}

internal sealed class AndroidViewHandle : INativeControlHostDestroyableControlHandle
{
    private View _view;

    public AndroidViewHandle(View view)
    {
        _view = view;
    }

    public IntPtr Handle => _view?.Handle ?? IntPtr.Zero;
    public string HandleDescriptor => "JavaHandle";

    public void Destroy()
    {
        _view?.Dispose();
        _view = null;
    }

    ~AndroidViewHandle()
    {
        Destroy();
    }
}
#endif
