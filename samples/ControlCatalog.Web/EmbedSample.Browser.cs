using System;

using Avalonia;
using Avalonia.Platform;
using Avalonia.Web.Blazor;

using ControlCatalog.Pages;

using Microsoft.JSInterop;

namespace ControlCatalog.Web;

public class EmbedSampleWeb : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        var runtime = AvaloniaLocator.Current.GetRequiredService<IJSInProcessRuntime>();

        if (isSecond)
        {
            var iframe = runtime.Invoke<IJSInProcessObjectReference>("document.createElement", "iframe");
            iframe.InvokeVoid("setAttribute", "src", "https://www.youtube.com/embed/kZCIporjJ70");

            return new JSObjectControlHandle(iframe);
        }
        else
        {
            // window.createAppButton source is defined in "app.js" file.
            var button = runtime.Invoke<IJSInProcessObjectReference>("window.createAppButton");

            return new JSObjectControlHandle(button);
        }
    }
}
