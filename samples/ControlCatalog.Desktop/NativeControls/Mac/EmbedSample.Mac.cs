using System;
using System.Runtime.InteropServices;

using Avalonia.Controls.Platform;
using Avalonia.Platform;
using ControlCatalog.Pages;

namespace ControlCatalog.Desktop;

public class EmbedSampleMac : INativeDemoControl
{
    private static bool? s_webKitLoaded;

    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        s_webKitLoaded ??= NativeLibrary.TryLoad("/System/Library/Frameworks/WebKit.framework/WebKit", out _);

        if (s_webKitLoaded is false)
        {
            return createDefault();
        }

        // alloc
        var webView = ObjC.MsgSend(ObjC.GetClass("WKWebView"), ObjC.GetUid("alloc"));
        // init
        webView = ObjC.MsgSend(webView, ObjC.GetUid("init"));

        // ns url
        var urlNsString = ObjC.CreateString(isSecond ? "https://bing.com" : "https://google.com/");
        var url = ObjC.MsgSend(
            ObjC.GetClass("NSURL"), ObjC.GetUid("URLWithString:"),
            urlNsString);

        // ns url request
        var request = ObjC.MsgSend(ObjC.GetClass("NSURLRequest"), ObjC.GetUid("requestWithURL:"), url);

        // load request
        ObjC.MsgSend(webView, ObjC.GetUid("loadRequest:"), request);

        return new MacOSViewHandle(webView);
    }

    private sealed class MacOSViewHandle(IntPtr view) : INativeControlHostDestroyableControlHandle
    {
        public IntPtr Handle { get; private set; } = view;
        public string HandleDescriptor => "NSView";

        public void Destroy()
        {
            if (Handle != IntPtr.Zero)
            {
                ObjC.MsgSend(Handle, ObjC.GetUid("release"));
                Handle = IntPtr.Zero;
            }
        }
    }
}
