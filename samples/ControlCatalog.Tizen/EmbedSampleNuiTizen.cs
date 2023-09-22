using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Tizen;
using ControlCatalog.Pages;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Tizen.Pims.Contacts.ContactsViews;

namespace ControlCatalog.Tizen;
public class EmbedSampleNuiTizen : INativeDemoControl
{
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        if (isSecond)
        {
            var webView = new WebView();
            webView.LoadUrl("https://avaloniaui.net/");
            return new NuiViewControlHandle(webView);
        }
        else
        {
            var clickCount = 0;
            var button = new Button
            {
                Text = "Hello world"
            };

            button.Clicked += (sender, e) =>
            {
                clickCount++;
                button.Text = $"Click count {clickCount}";
            };

            return new NuiViewControlHandle(button);
        }
    }
}
