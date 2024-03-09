using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Platform;
using Avalonia.Browser;

using ControlCatalog.Pages;
using System.Threading.Tasks;

namespace ControlCatalog.Browser;

public class EmbedSampleWeb : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        if (isSecond)
        {
            var iframe = EmbedInterop.CreateElement("iframe");
            iframe.SetProperty("src", "https://www.youtube.com/embed/kZCIporjJ70");

            return new JSObjectControlHandle(iframe);
        }
        else
        {
            var parentContainer = (JSObjectControlHandle)createDefault();

            AddButton(parentContainer.Object);

            return parentContainer;

            static async void AddButton(JSObject parent)
            {
                await JSHost.ImportAsync("embed.js", "./embed.js");
                EmbedInterop.AddAppButton(parent);
            } 
        }
    }
}

internal static partial class EmbedInterop
{
    [JSImport("globalThis.document.createElement")]
    public static partial JSObject CreateElement(string tagName);

    [JSImport("addAppButton", "embed.js")]
    public static partial void AddAppButton(JSObject parentObject);
}
