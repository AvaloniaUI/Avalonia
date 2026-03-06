using System;
using Avalonia.Platform;
using Avalonia.Threading;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace IntegrationTestApp.Embedding;

internal class MacOSTextBoxFactory : INativeTextBoxFactory
{
    public INativeTextBoxImpl CreateControl(IPlatformHandle parent)
    {
        MacHelper.EnsureInitialized();
        return new MacOSTextBox();
    }

    private class MacOSTextBox : NSTextView, INativeTextBoxImpl
    {
        private DispatcherTimer _timer;
        
        public MacOSTextBox()
        {
            TextStorage.Append(new("Native text box"));
            Handle = new MacOSViewHandle(this);
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(400);
            _timer.Tick += (_, _) =>
            {
                Hovered?.Invoke(this, EventArgs.Empty);
                _timer.Stop();
            };
        }

        public new IPlatformHandle Handle { get; }

        public string Text
        {
            get => TextStorage.Value;
            set => TextStorage.Replace(new NSRange(0, TextStorage.Length), value);
        }

        public event EventHandler? ContextMenuRequested;
        public event EventHandler? Hovered;
        public event EventHandler? PointerExited;

        public override void MouseEntered(NSEvent theEvent)
        {
            _timer.Stop();
            _timer.Start();
            base.MouseEntered(theEvent);
        }

        public override void MouseExited(NSEvent theEvent)
        {
            _timer.Stop();
            PointerExited?.Invoke(this, EventArgs.Empty);
            base.MouseExited(theEvent);
        }

        public override void MouseMoved(NSEvent theEvent)
        {
            _timer.Stop();
            _timer.Start();
            base.MouseMoved(theEvent);
        }

        public override void RightMouseDown(NSEvent theEvent)
        {
            ContextMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        public override void RightMouseUp(NSEvent theEvent)
        {
            // Don't call base to prevent default action.
        }
    }
}
