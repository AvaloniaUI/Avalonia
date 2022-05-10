#if __ANDROID__ || ANDROID
using System;
using System.IO;
using System.Diagnostics;
using Android.Views;
using Android.Webkit;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private IPlatformHandle CreateAndroid(IPlatformHandle parent)
    {
        var button = new Android.Widget.Button(Android.App.Application.Context) { Text = "Android button" };

        return new AndroidViewHandle(button);
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
