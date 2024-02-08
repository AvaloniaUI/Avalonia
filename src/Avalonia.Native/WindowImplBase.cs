using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class MacOSTopLevelWindowHandle : IPlatformHandle, IMacOSTopLevelPlatformHandle
    {
        IAvnWindowBase _native;

        public MacOSTopLevelWindowHandle(IAvnWindowBase native)
        {
            _native = native;
        }

        public IntPtr Handle => NSWindow;

        public string HandleDescriptor => "NSWindow";

        public IntPtr NSView => _native?.ObtainNSViewHandle() ?? IntPtr.Zero;

        public IntPtr NSWindow => _native?.ObtainNSWindowHandle() ?? IntPtr.Zero;

        public IntPtr GetNSViewRetained()
        {
            return _native?.ObtainNSViewHandleRetained() ?? IntPtr.Zero;
        }

        public IntPtr GetNSWindowRetained()
        {
            return _native?.ObtainNSWindowHandleRetained() ?? IntPtr.Zero;
        }
    }

    internal abstract class WindowBaseImpl : IWindowBaseImpl,
        IFramebufferPlatformSurface
    {
        protected readonly IAvaloniaNativeFactory _factory;
        protected IInputRoot _inputRoot;
        IAvnWindowBase _native;
        private object _syncRoot = new object();
        private readonly MouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;
        private readonly ICursorFactory _cursorFactory;
        private Size _savedLogicalSize;
        private double _savedScaling;
        private NativeControlHostImpl _nativeControlHost;
        private IStorageProvider _storageProvider;
        private PlatformBehaviorInhibition _platformBehaviorInhibition;
        private WindowTransparencyLevel _transparencyLevel = WindowTransparencyLevel.None;

        internal WindowBaseImpl(IAvaloniaNativeFactory factory)
        {
            _factory = factory;

            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            _mouse = new MouseDevice();
            _cursorFactory = AvaloniaLocator.Current.GetService<ICursorFactory>();
        }

        protected void Init(IAvnWindowBase window, IAvnScreens screens)
        {
            _native = window;

            Surfaces = new object[] { new GlPlatformSurface(window), new MetalPlatformSurface(window), this };
            Handle = new MacOSTopLevelWindowHandle(window);
            Screen = new ScreenImpl(screens);

            _savedLogicalSize = ClientSize;
            _savedScaling = RenderScaling;
            _nativeControlHost = new NativeControlHostImpl(_native.CreateNativeControlHost());
            _storageProvider = new SystemDialogs(this, _factory.CreateSystemDialogs());
            _platformBehaviorInhibition = new PlatformBehaviorInhibition(_factory.CreatePlatformBehaviorInhibition());

            var monitor = Screen.AllScreens.OrderBy(x => x.Scaling)
                    .FirstOrDefault(m => m.Bounds.Contains(Position));

            Resize(new Size(monitor.WorkingArea.Width * 0.75d, monitor.WorkingArea.Height * 0.7d), WindowResizeReason.Layout);
        }

        public IAvnWindowBase Native => _native;

        public Size ClientSize 
        {
            get
            {
                if (_native != null)
                {
                    var s = _native.ClientSize;
                    return new Size(s.Width, s.Height);
                }

                return default;
            }
        }

        public Size? FrameSize
        {
            get
            {
                if (_native != null)
                {
                    unsafe
                    {
                        var s = new AvnSize { Width = -1, Height = -1 };
                        _native.GetFrameSize(&s);
                        return s.Width < 0  && s.Height < 0 ? null : new Size(s.Width, s.Height);
                    }
                }

                return default;
            }
        }

        public IEnumerable<object> Surfaces { get; private set; }

        public INativeControlHostImpl NativeControlHost => _nativeControlHost;


        IFramebufferRenderTarget IFramebufferPlatformSurface.CreateFramebufferRenderTarget()
        {
            if (!Dispatcher.UIThread.CheckAccess())
                throw new RenderTargetNotReadyException();
            return new FramebufferRenderTarget(this, _native.CreateSoftwareRenderTarget());
        }

        class FramebufferRenderTarget : IFramebufferRenderTarget
        {
            private readonly WindowBaseImpl _parent;
            private IAvnSoftwareRenderTarget _target;

            public FramebufferRenderTarget(WindowBaseImpl parent, IAvnSoftwareRenderTarget target)
            {
                _parent = parent;
                _target = target;
            }

            public void Dispose()
            {
                lock (_parent._syncRoot)
                {
                    _target?.Dispose();
                    _target = null;
                }
            }
            
            public ILockedFramebuffer Lock()
            {
                var w = _parent._savedLogicalSize.Width * _parent._savedScaling;
                var h = _parent._savedLogicalSize.Height * _parent._savedScaling;
                var dpi = _parent._savedScaling * 96;
                return new DeferredFramebuffer(_target, cb =>
                {
                    lock (_parent._syncRoot)
                    {
                        if (_parent._native != null && _target != null)
                        {
                            cb(_parent._native);
                        }
                    }
                }, (int)w, (int)h, new Vector(dpi, dpi));
            }
        }

        public Action LostFocus { get; set; }
        
        public Action<Rect> Paint { get; set; }
        public Action<Size, WindowResizeReason> Resized { get; set; }
        public Action Closed { get; set; }
        public IMouseDevice MouseDevice => _mouse;
        public abstract IPopupImpl CreatePopup();

        public AutomationPeer GetAutomationPeer()
        {
            return _inputRoot is Control c ? ControlAutomationPeer.CreatePeerForElement(c) : null;
        }

        protected unsafe class WindowBaseEvents : NativeCallbackBase, IAvnWindowBaseEvents
        {
            private readonly WindowBaseImpl _parent;

            public WindowBaseEvents(WindowBaseImpl parent)
            {
                _parent = parent;
            }

            void IAvnWindowBaseEvents.Closed()
            {
                var n = _parent._native;
                try
                {
                    _parent?.Closed?.Invoke();
                }
                finally
                {
                    
                    _parent?.Dispose();
                    n?.Dispose();
                }
            }

            void IAvnWindowBaseEvents.Activated() => _parent.Activated?.Invoke();

            void IAvnWindowBaseEvents.Deactivated() => _parent.Deactivated?.Invoke();

            void IAvnWindowBaseEvents.Paint()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
                var s = _parent.ClientSize;
                _parent.Paint?.Invoke(new Rect(0, 0, s.Width, s.Height));
            }

            void IAvnWindowBaseEvents.Resized(AvnSize* size, AvnPlatformResizeReason reason)
            {
                if (_parent?._native != null)
                {
                    var s = new Size(size->Width, size->Height);
                    _parent._savedLogicalSize = s;
                    _parent.Resized?.Invoke(s, (WindowResizeReason)reason);
                }
            }

            void IAvnWindowBaseEvents.PositionChanged(AvnPoint position)
            {
                _parent.PositionChanged?.Invoke(position.ToAvaloniaPixelPoint());
            }

            void IAvnWindowBaseEvents.RawMouseEvent(AvnRawMouseEventType type, ulong timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
            {
                _parent.RawMouseEvent(type, timeStamp, modifiers, point, delta);
            }

            int IAvnWindowBaseEvents.RawKeyEvent(AvnRawKeyEventType type, ulong timeStamp, AvnInputModifiers modifiers, AvnKey key, AvnPhysicalKey physicalKey, string keySymbol)
            {
                return _parent.RawKeyEvent(type, timeStamp, modifiers, key, physicalKey, keySymbol).AsComBool();
            }

            int IAvnWindowBaseEvents.RawTextInputEvent(ulong timeStamp, string text)
            {
                return _parent.RawTextInputEvent(timeStamp, text).AsComBool();
            }

            void IAvnWindowBaseEvents.ScalingChanged(double scaling)
            {
                _parent._savedScaling = scaling;
                _parent.ScalingChanged?.Invoke(scaling);
            }

            void IAvnWindowBaseEvents.RunRenderPriorityJobs()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
            }
            
            void IAvnWindowBaseEvents.LostFocus()
            {
                _parent.LostFocus?.Invoke();
            }

            public AvnDragDropEffects DragEvent(AvnDragEventType type, AvnPoint position,
                AvnInputModifiers modifiers,
                AvnDragDropEffects effects,
                IAvnClipboard clipboard, IntPtr dataObjectHandle)
            {
                var device = AvaloniaLocator.Current.GetService<IDragDropDevice>();

                IDataObject dataObject = null;
                if (dataObjectHandle != IntPtr.Zero)
                    dataObject = GCHandle.FromIntPtr(dataObjectHandle).Target as IDataObject;
                
                using(var clipboardDataObject = new ClipboardDataObject(clipboard))
                {
                    if (dataObject == null)
                        dataObject = clipboardDataObject;
                    
                    var args = new RawDragEvent(device, (RawDragEventType)type,
                        _parent._inputRoot, position.ToAvaloniaPoint(), dataObject, (DragDropEffects)effects,
                        (RawInputModifiers)modifiers);
                    _parent.Input(args);
                    return (AvnDragDropEffects)args.Effects;
                }
            }

            IAvnAutomationPeer IAvnWindowBaseEvents.AutomationPeer
            {
                get => AvnAutomationPeer.Wrap(_parent.GetAutomationPeer());
            }
        }
       
        public void Activate()
        {
            _native?.Activate();
        }

        public bool RawTextInputEvent(ulong timeStamp, string text)
        {
            if (_inputRoot is null) 
                return false;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawTextInputEventArgs(_keyboard, timeStamp, _inputRoot, text);

            Input?.Invoke(args);

            return args.Handled;
        }

        public bool RawKeyEvent(
            AvnRawKeyEventType type,
            ulong timeStamp,
            AvnInputModifiers modifiers,
            AvnKey key,
            AvnPhysicalKey physicalKey,
            string keySymbol)
        {
            if (_inputRoot is null) 
                return false;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawKeyEventArgs(
                _keyboard,
                timeStamp,
                _inputRoot,
                (RawKeyEventType)type,
                (Key)key,
                (RawInputModifiers)modifiers,
                (PhysicalKey)physicalKey,
                keySymbol);

            Input?.Invoke(args);

            return args.Handled;
        }

        protected virtual bool ChromeHitTest(RawPointerEventArgs e)
        {
            return false;
        }

        public void RawMouseEvent(AvnRawMouseEventType type, ulong timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
        {
            if (_inputRoot is null) 
                return;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            switch (type)
            {
                case AvnRawMouseEventType.Wheel:
                    Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot,
                        point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;
                
                case AvnRawMouseEventType.Magnify:
                    Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Magnify, 
                        point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;
                    
                case AvnRawMouseEventType.Rotate:
                    Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Rotate, 
                        point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;
                
                case AvnRawMouseEventType.Swipe:
                    Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Swipe,
                        point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;

                default:
                    var e = new RawPointerEventArgs(_mouse, timeStamp, _inputRoot, (RawPointerEventType)type,
                        point.ToAvaloniaPoint(), (RawInputModifiers)modifiers);
                    
                    if(!ChromeHitTest(e))
                    {
                        Input?.Invoke(e);
                    }
                    break;
            }
        }

        public void Resize(Size clientSize, WindowResizeReason reason)
        {
            _native?.Resize(clientSize.Width, clientSize.Height, (AvnPlatformResizeReason)reason);
        }

        public Compositor Compositor => AvaloniaNativePlatform.Compositor;

        public virtual void Dispose()
        {
            _native?.Close();
            _native?.Dispose();
            _native = null;

            _nativeControlHost?.Dispose();
            _nativeControlHost = null;
            
            (Screen as ScreenImpl)?.Dispose();
            _mouse.Dispose();
        }


        public void Invalidate(Rect rect)
        {
            _native?.Invalidate(new AvnRect { Height = rect.Height, Width = rect.Width, X = rect.X, Y = rect.Y });
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }


        public virtual void Show(bool activate, bool isDialog)
        {
            _native?.Show(activate.AsComBool(), isDialog.AsComBool());
        }


        public PixelPoint Position
        {
            get => _native?.Position.ToAvaloniaPixelPoint() ?? default;
            set => _native?.SetPosition(value.ToAvnPoint());
        }

        public Point PointToClient(PixelPoint point)
        {
            return _native?.PointToClient(point.ToAvnPoint()).ToAvaloniaPoint() ?? default;
        }

        public PixelPoint PointToScreen(Point point)
        {
            return _native?.PointToScreen(point.ToAvnPoint()).ToAvaloniaPixelPoint() ?? default;
        }

        public void Hide()
        {
            _native?.Hide();
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _native?.BeginMoveDrag();
        }

        public Size MaxAutoSizeHint => Screen.AllScreens.Select(s => s.Bounds.Size.ToSize(1))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();

        public void SetTopmost(bool value)
        {
            _native?.SetTopMost(value.AsComBool());
        }

        public double RenderScaling => _native?.Scaling ?? 1;

        public double DesktopScaling => 1;

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public void SetCursor(ICursorImpl cursor)
        {
            if (_native == null)
            {
                return;
            }
            
            var newCursor = cursor as AvaloniaNativeCursor;
            newCursor = newCursor ?? (_cursorFactory.GetCursor(StandardCursorType.Arrow) as AvaloniaNativeCursor);
            _native.SetCursor(newCursor.Cursor);
        }

        public Action<PixelPoint> PositionChanged { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public IScreenImpl Screen { get; private set; }

        // TODO

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _native?.SetMinMaxSize(minSize.ToAvnSize(), maxSize.ToAvnSize());
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {

        }

        internal void BeginDraggingSession(AvnDragDropEffects effects, AvnPoint point, IAvnClipboard clipboard,
            IAvnDndResultCallback callback, IntPtr sourceHandle)
        {
            _native?.BeginDragAndDropOperation(effects, point, clipboard, callback, sourceHandle);
        }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels) 
        {
            foreach (var level in transparencyLevels)
            {
                AvnWindowTransparencyMode? mode = null;

                if (level == WindowTransparencyLevel.None)
                    mode = AvnWindowTransparencyMode.Opaque;
                if (level == WindowTransparencyLevel.Transparent)
                    mode = AvnWindowTransparencyMode.Transparent;
                else if (level == WindowTransparencyLevel.AcrylicBlur)
                    mode = AvnWindowTransparencyMode.Blur;

                if (mode.HasValue && level != TransparencyLevel)
                {
                    _native?.SetTransparencyMode(mode.Value);
                    TransparencyLevel = level;
                    return;
                }
            }

            // If we get here, we didn't find a supported level. Use the default of None.
            if (TransparencyLevel != WindowTransparencyLevel.None)
            {
                _native?.SetTransparencyMode(AvnWindowTransparencyMode.Opaque);
                TransparencyLevel = WindowTransparencyLevel.None;
            }
        }

        public WindowTransparencyLevel TransparencyLevel
        {
            get => _transparencyLevel;
            private set
            {
                if (_transparencyLevel != value)
                {
                    _transparencyLevel = value;
                    TransparencyLevelChanged?.Invoke(value);
                }
            }
        }

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
        {
            _native.SetFrameThemeVariant((AvnPlatformThemeVariant)themeVariant);
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0, 0);
        public virtual object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }
            
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(IPlatformBehaviorInhibition))
            {
                return _platformBehaviorInhibition;
            }

            if (featureType == typeof(IClipboard))
            {
                return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
            }

            if (featureType == typeof(ILauncher))
            {
                return new BclLauncher();
            }

            return null;
        }

        public IPlatformHandle Handle { get; private set; }
    }
}
