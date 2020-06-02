using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
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
        private double _extendTitleBarHeight = -1;

        internal WindowImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts,
            GlPlatformFeature glFeature) : base(opts, glFeature)
        {
            _factory = factory;
            _opts = opts;
            _glFeature = glFeature;
            using (var e = new WindowEvents(this))
            {
                var context = _opts.UseGpu ? glFeature?.DeferredContext : null;
                Init(_native = factory.CreateWindow(e, context?.Context), factory.CreateScreens(), context);
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
                if (_parent.Closing != null)
                {
                    return _parent.Closing();
                }

                return true;
            }

            void IAvnWindowEvents.WindowStateChanged(AvnWindowState state)
            {
                _parent.InvalidateExtendedMargins();

                _parent.WindowStateChanged?.Invoke((WindowState)state);
            }

            void IAvnWindowEvents.GotInputWhenDisabled()
            {
                _parent.GotInputWhenDisabled?.Invoke();
            }
        }

        public IAvnWindow Native => _native;

        public void CanResize(bool value)
        {
            _native.CanResize = value;
        }

        public void SetSystemDecorations(Controls.SystemDecorations enabled)
        {
            _native.Decorations = (Interop.SystemDecorations)enabled;
        }

        public void SetTitleBarColor(Avalonia.Media.Color color)
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

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; private set; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        private bool _isExtended;
        public bool IsClientAreaExtendedToDecorations => _isExtended;

        protected override bool ChromeHitTest (RawPointerEventArgs e)
        {
            if(_isExtended)
            {
                if(e.Type == RawPointerEventType.LeftButtonDown)
                {
                    var visual = (_inputRoot as Window).Renderer.HitTestFirst(e.Position, _inputRoot as Window, x =>
                            {
                                if (x is IInputElement ie && !ie.IsHitTestVisible)
                                {
                                    return false;
                                }
                                return true;
                            });

                    if(visual == null)
                    {
                        _native.BeginMoveDrag();
                    }
                }
            }

            return false;
        }

        private void InvalidateExtendedMargins ()
        {
            if(WindowState ==  WindowState.FullScreen)
            {
                ExtendedMargins = new Thickness();
            }
            else
            {
                ExtendedMargins = _isExtended ? new Thickness(0, _extendTitleBarHeight == -1 ? _native.GetExtendTitleBarHeight() : _extendTitleBarHeight, 0, 0) : new Thickness();
            }

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            _isExtended = extendIntoClientAreaHint;

            _native.SetExtendClientArea(extendIntoClientAreaHint);

            InvalidateExtendedMargins();
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
            if(hints.HasFlag(ExtendClientAreaChromeHints.PreferSystemChromeButtons))
            {
                hints |= ExtendClientAreaChromeHints.SystemChromeButtons;
            }
            
            _native.SetExtendClientAreaHints ((AvnExtendClientAreaChromeHints)hints);
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHeight = titleBarHeight;
            _native.SetExtendTitleBarHeight(titleBarHeight);

            ExtendedMargins = _isExtended ? new Thickness(0, titleBarHeight == -1 ? _native.GetExtendTitleBarHeight() : titleBarHeight, 0, 0) : new Thickness();

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

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

        public Action GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl parent)
        {
            _native.SetParent(((WindowImpl)parent).Native);
        }

        public void SetEnabled(bool enable)
        {
            _native.SetEnabled(enable);
        }
    }
}
