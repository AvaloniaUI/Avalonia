﻿using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.Native
{
    public class WindowImpl : WindowBaseImpl, IWindowImpl, ITopLevelImplWithNativeMenuExporter
    {
        private readonly IAvaloniaNativeFactory _factory;
        private readonly AvaloniaNativePlatformOptions _opts;
        private readonly GlPlatformFeature _glFeature;
        IAvnWindow _native;
        internal WindowImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts,
            GlPlatformFeature glFeature) : base(opts, glFeature)
        {
            _factory = factory;
            _opts = opts;
            _glFeature = glFeature;
            using (var e = new WindowEvents(this))
            {
                Init(_native = factory.CreateWindow(e,
                    _opts.UseGpu ? glFeature?.DeferredContext.Context : null), factory.CreateScreens());
            }

            NativeMenuExporter = new AvaloniaNativeMenuExporter(_native, factory);
        }

        class WindowEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly WindowImpl _parent;

            public WindowEvents(WindowImpl parent) : base(parent)
            {
                _parent = parent;
            }

            bool IAvnWindowEvents.Closing()
            {
                if(_parent.Closing != null)
                {
                    return _parent.Closing();
                }

                return true;
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
                _parent.WindowStateChanged?.Invoke((WindowState)state);
            }
        }

        public IAvnWindow Native => _native;

        public void ShowDialog(IWindowImpl window)
        {
            _native.ShowDialog(((WindowImpl)window).Native);
        }

        public void CanResize(bool value)
        {
            _native.CanResize = value;
        }

        public void SetSystemDecorations(Controls.SystemDecorations enabled)
        {
            _native.HasDecorations = (Interop.SystemDecorations)enabled;
        }

        public void SetTitleBarColor (Avalonia.Media.Color color)
        {
            _native.SetTitleBarColor(new AvnColor { Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B });
        }

        public void SetTitle(string title)
        {
            using (var buffer = new Utf8Buffer(title))
            {
                _native.SetTitle(buffer.DangerousGetHandle());
            }
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
            // NO OP On OSX
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            // NO OP on OSX
        }

        public Func<bool> Closing { get; set; }

        public ITopLevelNativeMenuExporter NativeMenuExporter { get; }

        public void Move(PixelPoint point) => Position = point;

        public override IPopupImpl CreatePopup() =>
            _opts.OverlayPopups ? null : new PopupImpl(_factory, _opts, _glFeature, this);
    }
}
