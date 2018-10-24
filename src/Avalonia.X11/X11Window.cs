using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    class X11Window : IWindowImpl, IPopupImpl
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly bool _popup;
        private readonly X11Info _x11;
        private readonly Action<XEvent> _eventHandler;
        private bool _invalidated;
        private XConfigureEvent? _configure;
        private IInputRoot _inputRoot;
        private IMouseDevice _mouse;
        private Point _position;
        private IntPtr _handle;
        private IntPtr _renderHandle;
        private bool _mapped;

        class InputEventContainer
        {
            public RawInputEventArgs Event;
        }
        private Queue<InputEventContainer> _inputQueue = new Queue<InputEventContainer>();
        private InputEventContainer _lastEvent;

        public X11Window(AvaloniaX11Platform platform, bool popup)
        {
            _platform = platform;
            _popup = popup;
            _x11 = platform.Info;
            _mouse = platform.MouseDevice;
            
            
            XSetWindowAttributes attr = new XSetWindowAttributes();
            var valueMask = default(SetWindowValuemask);

            attr.backing_store = 1;
            attr.bit_gravity = Gravity.NorthWestGravity;
            attr.win_gravity = Gravity.NorthWestGravity;
            valueMask |= SetWindowValuemask.BackPixel | SetWindowValuemask.BorderPixel
                         | SetWindowValuemask.BackPixmap | SetWindowValuemask.BackingStore
                         | SetWindowValuemask.BitGravity | SetWindowValuemask.WinGravity;

            if (popup)
            {
                attr.override_redirect = true;
                valueMask |= SetWindowValuemask.OverrideRedirect;
            }
            
            _handle = XCreateWindow(_x11.Display, _x11.RootWindow, 10, 10, 300, 200, 0,
                24,
                (int)CreateWindowArgs.InputOutput, IntPtr.Zero, 
                new UIntPtr((uint)valueMask), ref attr);
            _renderHandle = XCreateWindow(_x11.Display, _handle, 0, 0, 300, 200, 0, 24,
                (int)CreateWindowArgs.InputOutput,
                IntPtr.Zero,
                new UIntPtr((uint)(SetWindowValuemask.BorderPixel | SetWindowValuemask.BitGravity |
                                   SetWindowValuemask.WinGravity | SetWindowValuemask.BackingStore)), ref attr);
                
            Handle = new PlatformHandle(_handle, "XID");
            ClientSize = new Size(400, 400);
            _eventHandler = OnEvent;
            platform.Windows[_handle] = _eventHandler;
            XSelectInput(_x11.Display, _handle,
                new IntPtr(0xffffff 
                           ^ (int)XEventMask.SubstructureRedirectMask 
                           ^ (int)XEventMask.ResizeRedirectMask
                           ^ (int)XEventMask.PointerMotionHintMask));
            var protocols = new[]
            {
                _x11.Atoms.WM_DELETE_WINDOW
            };
            XSetWMProtocols(_x11.Display, _handle, protocols, protocols.Length);
            var feature = (EglGlPlatformFeature)AvaloniaLocator.Current.GetService<IWindowingPlatformGlFeature>();
            var surfaces = new List<object>
            {
                new X11FramebufferSurface(_x11.DeferredDisplay, _handle)
            };
            if (feature != null)
                surfaces.Insert(0,
                    new EglGlPlatformSurface((EglDisplay)feature.Display, feature.DeferredContext,
                        new SurfaceInfo(_x11.DeferredDisplay, _handle, _renderHandle)));
            Surfaces = surfaces.ToArray();
            UpdateWmHits();
            XFlush(_x11.Display);
        }

        class SurfaceInfo  : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
        {
            private readonly IntPtr _display;
            private readonly IntPtr _parent;

            public SurfaceInfo(IntPtr display, IntPtr parent, IntPtr xid)
            {
                _display = display;
                _parent = parent;
                Handle = xid;
            }
            public IntPtr Handle { get; }

            public System.Drawing.Size PixelSize
            {
                get
                {
                    XLockDisplay(_display);
                    XGetGeometry(_display, _parent, out var geo);
                    XResizeWindow(_display, Handle, geo.width, geo.height);
                    XFlush(_display);
                    XSync(_display, true);
                    XUnlockDisplay(_display);
                    return new System.Drawing.Size(geo.width, geo.height);
                }
            }

            public double Scaling { get; } = 1;
        }

        void UpdateWmHits()
        {
            var functions = MotifFunctions.All;
            var decorations = MotifDecorations.All;

            if (_popup || !_systemDecorations)
            {
                functions = 0;
                decorations = 0;
            }

            if (!_canResize)
            {
                functions ^= MotifFunctions.Resize | MotifFunctions.Maximize;
                decorations ^= MotifDecorations.Maximize | MotifDecorations.ResizeH;
            }

            var hints = new MotifWmHints();
            hints.flags = new IntPtr((int)(MotifFlags.Decorations | MotifFlags.Functions));

            hints.decorations = new IntPtr((int)decorations);
            hints.functions = new IntPtr((int)functions);
            
            XChangeProperty(_x11.Display, _handle,
                _x11.Atoms._MOTIF_WM_HINTS, _x11.Atoms._MOTIF_WM_HINTS, 32,
                PropertyMode.Replace, ref hints, 5);
        }
        
        public Size ClientSize { get; private set; }
        public double Scaling { get; } = 1;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Action Closed { get; set; }
        public Action<Point> PositionChanged { get; set; }

        public IRenderer CreateRenderer(IRenderRoot root) =>
            new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());

        unsafe void OnEvent(XEvent ev)
        {
            if (ev.type == XEventName.MapNotify)
            {
                _mapped = true;
                XMapWindow(_x11.Display, _renderHandle);
            }
            else if (ev.type == XEventName.UnmapNotify)
                _mapped = false;
            else if (ev.type == XEventName.Expose)
                DoPaint();
            else if (ev.type == XEventName.FocusIn)
                Activated?.Invoke();
            else if (ev.type == XEventName.FocusOut)
                Deactivated?.Invoke();
            else if (ev.type == XEventName.MotionNotify)
                MouseEvent(RawMouseEventType.Move, ev, ev.MotionEvent.state);

            else if (ev.type == XEventName.ButtonPress)
            {
                if (ev.ButtonEvent.button < 4)
                    MouseEvent(ev.ButtonEvent.button == 1 ? RawMouseEventType.LeftButtonDown
                        : ev.ButtonEvent.button == 2 ? RawMouseEventType.MiddleButtonDown
                        : RawMouseEventType.RightButtonDown, ev, ev.ButtonEvent.state);
                else
                {
                    var delta = ev.ButtonEvent.button == 4
                        ? new Vector(0, 1)
                        : ev.ButtonEvent.button == 5
                            ? new Vector(0, -1)
                            : ev.ButtonEvent.button == 6
                                ? new Vector(1, 0)
                                : new Vector(-1, 0);
                    ScheduleInput(new RawMouseWheelEventArgs(_mouse, (ulong)ev.ButtonEvent.time.ToInt64(),
                        _inputRoot, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), delta,
                        TranslateModifiers(ev.ButtonEvent.state)));
                }
                
            }
            else if (ev.type == XEventName.ButtonRelease)
            {
                if (ev.ButtonEvent.button < 4)
                    MouseEvent(ev.ButtonEvent.button == 1 ? RawMouseEventType.LeftButtonUp
                        : ev.ButtonEvent.button == 2 ? RawMouseEventType.MiddleButtonUp
                        : RawMouseEventType.RightButtonUp, ev, ev.ButtonEvent.state);
            }
            else if (ev.type == XEventName.ConfigureNotify)
            {
                var needEnqueue = (_configure == null);
                _configure = ev.ConfigureEvent;
                if (needEnqueue)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_configure == null)
                            return;
                        var cev = _configure.Value;
                        _configure = null;
                        var nsize = new Size(cev.width, cev.height);
                        var npos = new Point(cev.x, cev.y);
                        var changedSize = ClientSize != nsize;
                        var changedPos = npos != _position;
                        ClientSize = nsize;
                        _position = npos;
                        if (changedSize)
                            Resized?.Invoke(nsize);
                        if (changedPos)
                            PositionChanged?.Invoke(npos);
                    });
            }
            else if (ev.type == XEventName.DestroyNotify)
            {
                Cleanup();
            }
            else if (ev.type == XEventName.ClientMessage)
            {
                if (ev.ClientMessageEvent.message_type == _x11.Atoms.WM_PROTOCOLS)
                {
                    if (ev.ClientMessageEvent.ptr1 == _x11.Atoms.WM_DELETE_WINDOW)
                    {
                        if (Closing?.Invoke() != true)
                            Dispose();
                    }

                }
            }
        }
        
        

        InputModifiers TranslateModifiers(XModifierMask state)
        {
            var rv = default(InputModifiers);
            if (state.HasFlag(XModifierMask.Button1Mask))
                rv |= InputModifiers.LeftMouseButton;
            if (state.HasFlag(XModifierMask.Button2Mask))
                rv |= InputModifiers.RightMouseButton;
            if (state.HasFlag(XModifierMask.Button2Mask))
                rv |= InputModifiers.MiddleMouseButton;
            if (state.HasFlag(XModifierMask.ShiftMask))
                rv |= InputModifiers.Shift;
            if (state.HasFlag(XModifierMask.ControlMask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(XModifierMask.Mod1Mask))
                rv |= InputModifiers.Alt;
            if (state.HasFlag(XModifierMask.Mod4Mask))
                rv |= InputModifiers.Windows;
            return rv;
        }
        
        private bool _systemDecorations = true;
        private bool _canResize = true;

        void ScheduleInput(RawInputEventArgs args)
        {
            _lastEvent = new InputEventContainer() {Event = args};
            _inputQueue.Enqueue(_lastEvent);
            if (_inputQueue.Count == 1)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    while (_inputQueue.Count > 0)
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
                        var ev = _inputQueue.Dequeue();
                        Input?.Invoke(ev.Event);
                    }
                }, DispatcherPriority.Input);
            }
        }
        
        void MouseEvent(RawMouseEventType type, XEvent ev, XModifierMask mods)
        {
            _x11.LastActivityTimestamp = ev.ButtonEvent.time;
            var mev = new RawMouseEventArgs(
                _mouse, (ulong)ev.ButtonEvent.time.ToInt64(), _inputRoot,
                type, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), TranslateModifiers(mods)); 
            if(type == RawMouseEventType.Move && _inputQueue.Count>0 && _lastEvent.Event is RawMouseEventArgs ma)
                if (ma.Type == RawMouseEventType.Move)
                {
                    _lastEvent.Event = mev;
                    return;
                }
            ScheduleInput(mev);
        }

        void DoPaint()
        {
            _invalidated = false;
            Paint?.Invoke(new Rect());
        }
        
        public void Invalidate(Rect rect)
        {
            if(_invalidated)
                return;
            _invalidated = true;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_mapped)
                    DoPaint();
            });
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _handle);
                Cleanup();
            }
        }

        void Cleanup()
        {
            if (_handle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _handle);
                _platform.Windows.Remove(_handle);
                _handle = IntPtr.Zero;
                Closed?.Invoke();
            }

            if (_renderHandle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _renderHandle);
                _renderHandle = IntPtr.Zero;
            }
        }
        
        
        public void Show() => XMapWindow(_x11.Display, _handle);

        public void Hide() => XUnmapWindow(_x11.Display, _handle);
        
        
        public Point PointToClient(Point point) => new Point(point.X - _position.X, point.Y - _position.Y);

        public Point PointToScreen(Point point) => new Point(point.X + _position.X, point.Y + _position.Y);
        
        public void SetSystemDecorations(bool enabled)
        {
            _systemDecorations = enabled;
            UpdateWmHits();
        }
        
                
        public void Resize(Size clientSize)
        {
            if (clientSize == ClientSize)
                return;
            var changes = new XWindowChanges();
            changes.width = (int)clientSize.Width;
            changes.height = (int)clientSize.Height;
            var needResize = clientSize != ClientSize;
            XConfigureWindow(_x11.Display, _handle, ChangeWindowFlags.CWHeight | ChangeWindowFlags.CWWidth,
                ref changes);
            XFlush(_x11.Display);

            if (_popup && needResize)
            {
                ClientSize = clientSize;
                Resized?.Invoke(clientSize);
            }
        }
        
        public void CanResize(bool value)
        {
            _canResize = value;
            UpdateWmHits();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public IPlatformHandle Handle { get; }
        
        public Point Position
        {
            get => _position;
            set
            {
                var changes = new XWindowChanges();
                changes.x = (int)value.X;
                changes.y = (int)value.Y;
                XConfigureWindow(_x11.Display, _handle, ChangeWindowFlags.CWX | ChangeWindowFlags.CWY,
                    ref changes);
                XFlush(_x11.Display);

            }
        }

        public IMouseDevice MouseDevice => _mouse;
       
        public unsafe void Activate()
        {
            if (_x11.Atoms._NET_ACTIVE_WINDOW != IntPtr.Zero)
            {

                var ev = new XEvent
                {
                    AnyEvent = 
                    {
                        type = XEventName.ClientMessage,
                        window = _handle,
                    },
                    ClientMessageEvent = {
                    message_type = _x11.Atoms._NET_ACTIVE_WINDOW,
                    format = 32,
                        ptr1 = new IntPtr(1),
                        ptr2 = _x11.LastActivityTimestamp
                        
                    }
                };

                XSendEvent(_x11.Display, _x11.RootWindow, false,
                    new IntPtr((int)(XEventMask.SubstructureRedirectMask | XEventMask.StructureNotifyMask)), ref ev);
            }
            else
            {
                XRaiseWindow(_x11.Display, _handle);
                XSetInputFocus(_x11.Display, _handle, 0, IntPtr.Zero);
            }
        }

        
        public IScreenImpl Screen { get; } = new ScreenStub();
        public Size MaxClientSize { get; } = new Size(1920, 1280);
        
       
        public void BeginMoveDrag()
        {
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
        }
        
        public void SetTitle(string title)
        {
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            
        }

        public void SetTopmost(bool value)
        {
            
        }

        public IDisposable ShowDialog()
        {
            return Disposable.Empty;
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public void ShowTaskbarIcon(bool value)
        {
        }



        
    }
}
