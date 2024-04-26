using System;
using System.Text;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.WebKit;

namespace IntegrationTestApp.Embedding;

internal class MacOSTextBoxFactory : INativeControlFactory
{
    public IPlatformHandle CreateControl(IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        MacHelper.EnsureInitialized();

        var textView = new NSTextView();
        textView.TextStorage.Append(new("macOS TextView"));

        return new MacOSViewHandle(textView);
    }
}
