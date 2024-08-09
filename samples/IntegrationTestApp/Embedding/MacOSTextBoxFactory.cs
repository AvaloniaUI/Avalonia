using System;
using Avalonia.Platform;
using MonoMac.AppKit;

namespace IntegrationTestApp.Embedding;

internal class MacOSTextBoxFactory : INativeTextBoxFactory
{
    public INativeTextBoxImpl CreateControl(IPlatformHandle parent)
    {
        MacHelper.EnsureInitialized();
        return new MacOSTextBox();
    }

    private class MacOSTextBox : INativeTextBoxImpl
    {
        public MacOSTextBox()
        {
            var textView = new NSTextView();
            textView.TextStorage.Append(new("Native text box"));
            Handle = new MacOSViewHandle(textView);
        }

        public IPlatformHandle Handle { get; }

        public event EventHandler? ContextMenuRequested;
        public event EventHandler? Hovered;
        public event EventHandler? PointerExited;
    }
}
