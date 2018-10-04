using System;
using Avalonia.Controls;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        IAvnWindow _native;
        public WindowImpl(IAvaloniaNativeFactory factory)
        {
            using (var e = new WindowEvents(this))
                Init(_native = factory.CreateWindow(e), factory.CreateScreens());
        }

        class WindowEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly WindowImpl _parent;

            public WindowEvents(WindowImpl parent) : base(parent)
            {
                _parent = parent;
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
                _parent.WindowStateChanged?.Invoke((WindowState)state);
            }
        }

        public IAvnWindow Native => _native;

        public IDisposable ShowDialog()
        {
            return _native.ShowDialog();
        }


        public void CanResize(bool value)
        {
            _native.CanResize = value;
        }

        public void SetSystemDecorations(bool enabled)
        {
            _native.HasDecorations = enabled;
        }

        public void SetTitle(string title)
        {
        }

        public WindowState WindowState
        {
            get
            {
                return (WindowState)_native.GetWindowState();
            }
            set
            {
                _native.SetWindowState((AvnWindowState)value);
            }
        }

        public Action<WindowState> WindowStateChanged { get; set; }

        public void ShowTaskbarIcon(bool value)
        {
        }
        public void SetIcon(IWindowIconImpl icon)
        {
        }
        public Func<bool> Closing { get; set; }
    }
}
