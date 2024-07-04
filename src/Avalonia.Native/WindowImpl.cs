using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class WindowImpl : WindowBaseImpl, IWindowImpl
    {
        private readonly AvaloniaNativePlatformOptions _opts;
        IAvnWindow _native;
        private double _extendTitleBarHeight = -1;
        private DoubleClickHelper _doubleClickHelper;
        private readonly ITopLevelNativeMenuExporter _nativeMenuExporter;
        private bool _canResize = true;

        internal WindowImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts) : base(factory)
        {
            _opts = opts;
            _doubleClickHelper = new DoubleClickHelper();
            
            using (var e = new WindowEvents(this))
            {
                Init(new MacOSTopLevelHandle(_native = factory.CreateWindow(e)), factory.CreateScreens());
            }

            _nativeMenuExporter = new AvaloniaNativeMenuExporter(_native, factory);
        }

        internal sealed override void Init(MacOSTopLevelHandle handle, IAvnScreens screens)
        {
            base.Init(handle, screens);
        }

        class WindowEvents : WindowBaseEvents, IAvnWindowEvents
        {
            readonly WindowImpl _parent;

            public WindowEvents(WindowImpl parent) : base(parent)
            {
                _parent = parent;
            }

            int IAvnWindowEvents.Closing()
            {
                if (_parent.Closing != null)
                {
                    return _parent.Closing(WindowCloseReason.WindowClosing).AsComBool();
                }

                return true.AsComBool();
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
        
        public new IAvnWindow Native => _native;

        public void CanResize(bool value)
        {
            _canResize = value;
            _native.SetCanResize(value.AsComBool());
        }

        public void SetSystemDecorations(Controls.SystemDecorations enabled)
        {
            _native.SetDecorations((Interop.SystemDecorations)enabled);
        }

        public void SetTitleBarColor(Avalonia.Media.Color color)
        {
            _native.SetTitleBarColor(new AvnColor { Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B });
        }

        public void SetTitle(string title)
        {
            _native.SetTitle(title ?? "");
        }

        public WindowState WindowState
        {
            get => (WindowState)_native.WindowState;
            set => _native.SetWindowState((AvnWindowState)value);
        }

        public Action<WindowState> WindowStateChanged { get; set; }        

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; private set; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        public IntPtr? ZOrder => _native.WindowZOrder;

        private bool _isExtended;
        public bool IsClientAreaExtendedToDecorations => _isExtended;

        public override void Show(bool activate, bool isDialog)
        {
            base.Show(activate, isDialog);
            
            InvalidateExtendedMargins();
        }

        protected override bool ChromeHitTest (RawPointerEventArgs e)
        {
            if(_isExtended)
            {
                if(e.Type == RawPointerEventType.LeftButtonDown)
                {
                    var window = _inputRoot as Window;
                    var visual = window?.Renderer.HitTestFirst(e.Position, window, x =>
                            {
                                if (x is IInputElement ie && (!ie.IsHitTestVisible || !ie.IsEffectivelyVisible))
                                {
                                    return false;
                                }
                                return true;
                            });

                    if(visual == null)
                    {
                        if (_doubleClickHelper.IsDoubleClick(e.Timestamp, e.Position))
                        {
                            if (_canResize)
                            {
                                WindowState = WindowState is WindowState.Maximized or WindowState.FullScreen ?
                                    WindowState.Normal : WindowState.Maximized;
                            }
                        }
                        else
                        {
                            _native.BeginMoveDrag();   
                        }
                    }
                }
            }

            return false;
        }
        
        private void InvalidateExtendedMargins()
        {
            if (WindowState ==  WindowState.FullScreen)
            {
                ExtendedMargins = new Thickness();
            }
            else
            {
                ExtendedMargins = _isExtended ? new Thickness(0, _extendTitleBarHeight == -1 ? _native.ExtendTitleBarHeight : _extendTitleBarHeight, 0, 0) : new Thickness();
            }

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            _isExtended = extendIntoClientAreaHint;

            _native.SetExtendClientArea(extendIntoClientAreaHint.AsComBool());

            InvalidateExtendedMargins();
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {   
            _native.SetExtendClientAreaHints ((AvnExtendClientAreaChromeHints)hints);
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHeight = titleBarHeight;
            _native.SetExtendTitleBarHeight(titleBarHeight);

            ExtendedMargins = _isExtended ? new Thickness(0, titleBarHeight == -1 ? _native.ExtendTitleBarHeight : titleBarHeight, 0, 0) : new Thickness();

            ExtendClientAreaToDecorationsChanged?.Invoke(_isExtended);
        }

        /// <inheritdoc/>
        public bool NeedsManagedDecorations => false;

        public void ShowTaskbarIcon(bool value)
        {
            // NO OP On OSX
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            // NO OP on OSX
        }

        public Func<WindowCloseReason, bool> Closing { get; set; }

        public void Move(PixelPoint point) => Position = point;

        public override IPopupImpl CreatePopup() =>
            _opts.OverlayPopups ? null : new PopupImpl(Factory, this);

        public Action GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl parent)
        {
            _native.SetParent(((WindowImpl)parent)?.Native);
        }

        public void SetEnabled(bool enable)
        {
            _native.SetEnabled(enable.AsComBool());

            // Showing a dialog should result in mouse capture being lost. macOS doesn't have the concept of mouse
            // capture, so no we have no OS-level event to hook into. Instead, release the mouse capture when the
            // owner window is disabled. This behavior matches win32, which sends a WM_CANCELMODE message when
            // EnableWindow(hWnd, false) is called from SetEnabled.
            if (!enable && MouseDevice is MouseDevice mouse)
                mouse.PlatformCaptureLost();
        }

        public override object TryGetFeature(Type featureType)
        {
            if(featureType == typeof(ITextInputMethodImpl))
            {
                return InputMethod;
            } 
            
            if (featureType == typeof(ITopLevelNativeMenuExporter))
            {
                return _nativeMenuExporter;
            }
            
            return base.TryGetFeature(featureType);
        }

        public void GetWindowsZOrder(Span<Window> windows, Span<long> zOrder)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                zOrder[i] = (windows[i].PlatformImpl as WindowImpl)?.ZOrder?.ToInt64() ?? 0;
            }
        }
    }
}
