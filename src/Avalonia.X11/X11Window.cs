using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.X11.Glx;
using static Avalonia.X11.XLib;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace Avalonia.X11
{
    unsafe class X11Window : IWindowImpl, IPopupImpl, IXI2Client
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly bool _popup;
        private readonly X11Info _x11;
        private bool _invalidated;
        private XConfigureEvent? _configure;
        private PixelPoint? _configurePoint;
        private bool _triggeredExpose;
        private IInputRoot _inputRoot;
        private readonly IMouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;
        private PixelPoint? _position;
        private PixelSize _realSize;
        private IntPtr _handle;
        private IntPtr _xic;
        private IntPtr _renderHandle;
        private bool _mapped;
        private HashSet<X11Window> _transientChildren = new HashSet<X11Window>();
        private X11Window _transientParent;
        public object SyncRoot { get; } = new object();

        class InputEventContainer
        {
            public RawInputEventArgs Event;
        }
        private readonly Queue<InputEventContainer> _inputQueue = new Queue<InputEventContainer>();
        private InputEventContainer _lastEvent;
        private bool _useRenderWindow = false;
        public X11Window(AvaloniaX11Platform platform, bool popup)
        {
            _platform = platform;
            _popup = popup;
            _x11 = platform.Info;
            _mouse = platform.MouseDevice;
            _keyboard = platform.KeyboardDevice;

            var glfeature = AvaloniaLocator.Current.GetService<IWindowingPlatformGlFeature>();
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

            XVisualInfo? visualInfo = null;

            // OpenGL seems to be do weird things to it's current window which breaks resize sometimes
            _useRenderWindow = glfeature != null;
            
            var glx = glfeature as GlxGlPlatformFeature;
            if (glx != null)
                visualInfo = *glx.Display.VisualInfo;
            else if (glfeature == null)
                visualInfo = _x11.TransparentVisualInfo;

            var egl = glfeature as EglGlPlatformFeature;
            
            var visual = IntPtr.Zero;
            var depth = 24;
            if (visualInfo != null)
            {
                visual = visualInfo.Value.visual;
                depth = (int)visualInfo.Value.depth;
                attr.colormap = XCreateColormap(_x11.Display, _x11.RootWindow, visualInfo.Value.visual, 0);
                valueMask |= SetWindowValuemask.ColorMap;   
            }

            _handle = XCreateWindow(_x11.Display, _x11.RootWindow, 10, 10, 300, 200, 0,
                depth,
                (int)CreateWindowArgs.InputOutput, 
                visual,
                new UIntPtr((uint)valueMask), ref attr);

            if (_useRenderWindow)
                _renderHandle = XCreateWindow(_x11.Display, _handle, 0, 0, 300, 200, 0, depth,
                    (int)CreateWindowArgs.InputOutput,
                    visual,
                    new UIntPtr((uint)(SetWindowValuemask.BorderPixel | SetWindowValuemask.BitGravity |
                                       SetWindowValuemask.WinGravity | SetWindowValuemask.BackingStore)), ref attr);
            else
                _renderHandle = _handle;
                
            Handle = new PlatformHandle(_handle, "XID");
            _realSize = new PixelSize(300, 200);
            platform.Windows[_handle] = OnEvent;
            XEventMask ignoredMask = XEventMask.SubstructureRedirectMask
                                     | XEventMask.ResizeRedirectMask
                                     | XEventMask.PointerMotionHintMask;
            if (platform.XI2 != null)
                ignoredMask |= platform.XI2.AddWindow(_handle, this);
            var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
            XSelectInput(_x11.Display, _handle, mask);
            var protocols = new[]
            {
                _x11.Atoms.WM_DELETE_WINDOW
            };
            XSetWMProtocols(_x11.Display, _handle, protocols, protocols.Length);
            XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_WINDOW_TYPE, _x11.Atoms.XA_ATOM,
                32, PropertyMode.Replace, new[] {_x11.Atoms._NET_WM_WINDOW_TYPE_NORMAL}, 1);

            if (platform.Options.WmClass != null)
                SetWmClass(platform.Options.WmClass);

            var surfaces = new List<object>
            {
                new X11FramebufferSurface(_x11.DeferredDisplay, _renderHandle, 
                   depth, () => Scaling)
            };
            
            if (egl != null)
                surfaces.Insert(0,
                    new EglGlPlatformSurface((EglDisplay)egl.Display, egl.DeferredContext,
                        new SurfaceInfo(this, _x11.DeferredDisplay, _handle, _renderHandle)));
            if (glx != null)
                surfaces.Insert(0, new GlxGlPlatformSurface(glx.Display, glx.DeferredContext,
                    new SurfaceInfo(this, _x11.Display, _handle, _renderHandle)));
            
            Surfaces = surfaces.ToArray();
            UpdateMotifHints();
            _xic = XCreateIC(_x11.Xim, XNames.XNInputStyle, XIMProperties.XIMPreeditNothing | XIMProperties.XIMStatusNothing,
                XNames.XNClientWindow, _handle, IntPtr.Zero);
            XFlush(_x11.Display);
        }

        class SurfaceInfo  : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
        {
            private readonly X11Window _window;
            private readonly IntPtr _display;
            private readonly IntPtr _parent;

            public SurfaceInfo(X11Window window, IntPtr display, IntPtr parent, IntPtr xid)
            {
                _window = window;
                _display = display;
                _parent = parent;
                Handle = xid;
            }
            public IntPtr Handle { get; }

            public PixelSize Size
            {
                get
                {
                    XLockDisplay(_display);
                    XGetGeometry(_display, _parent, out var geo);
                    XResizeWindow(_display, Handle, geo.width, geo.height);
                    XUnlockDisplay(_display);
                    return new PixelSize(geo.width, geo.height);
                }
            }

            public double Scaling => _window.Scaling;
        }

        void UpdateMotifHints()
        {
            var functions = MotifFunctions.Move | MotifFunctions.Close | MotifFunctions.Resize |
                            MotifFunctions.Minimize | MotifFunctions.Maximize;
            var decorations = MotifDecorations.Menu | MotifDecorations.Title | MotifDecorations.Border |
                              MotifDecorations.Maximize | MotifDecorations.Minimize | MotifDecorations.ResizeH;

            if (_popup || !_systemDecorations)
            {
                decorations = 0;
            }

            if (!_canResize)
            {
                functions &= ~(MotifFunctions.Resize | MotifFunctions.Maximize);
                decorations &= ~(MotifDecorations.Maximize | MotifDecorations.ResizeH);
            }

            var hints = new MotifWmHints
            {
                flags = new IntPtr((int)(MotifFlags.Decorations | MotifFlags.Functions)),
                decorations = new IntPtr((int)decorations),
                functions = new IntPtr((int)functions)
            };

            XChangeProperty(_x11.Display, _handle,
                _x11.Atoms._MOTIF_WM_HINTS, _x11.Atoms._MOTIF_WM_HINTS, 32,
                PropertyMode.Replace, ref hints, 5);
        }

        void UpdateSizeHints(PixelSize? preResize)
        {
            var min = _minMaxSize.minSize;
            var max = _minMaxSize.maxSize;

            if (!_canResize)
                max = min = _realSize;
            
            if (preResize.HasValue)
            {
                var desired = preResize.Value;
                max = new PixelSize(Math.Max(desired.Width, max.Width), Math.Max(desired.Height, max.Height));
                min = new PixelSize(Math.Min(desired.Width, min.Width), Math.Min(desired.Height, min.Height));
            }

            var hints = new XSizeHints
            {
                min_width = min.Width,
                min_height = min.Height
            };
            hints.height_inc = hints.width_inc = 1;
            var flags = XSizeHintsFlags.PMinSize | XSizeHintsFlags.PResizeInc;
            // People might be passing double.MaxValue
            if (max.Width < 100000 && max.Height < 100000)
            {
                hints.max_width = max.Width;
                hints.max_height = max.Height;
                flags |= XSizeHintsFlags.PMaxSize;
            }

            hints.flags = (IntPtr)flags;

            XSetWMNormalHints(_x11.Display, _handle, ref hints);
        }

        public Size ClientSize => new Size(_realSize.Width / Scaling, _realSize.Height / Scaling);

        public double Scaling
        {
            get
            {
                lock (SyncRoot)
                    return _scaling;

            }
            private set => _scaling = value;
        }

        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        //TODO
        public Action<double> ScalingChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Action Closed { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }

        public IRenderer CreateRenderer(IRenderRoot root) =>
            new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());

        void OnEvent(XEvent ev)
        {
            lock (SyncRoot)
                OnEventSync(ev);
        }
        void OnEventSync(XEvent ev)
        {
            if(XFilterEvent(ref ev, _handle))
                return;
            if (ev.type == XEventName.MapNotify)
            {
                _mapped = true;
                if (_useRenderWindow)
                    XMapWindow(_x11.Display, _renderHandle);
            }
            else if (ev.type == XEventName.UnmapNotify)
                _mapped = false;
            else if (ev.type == XEventName.Expose)
            {
                if (!_triggeredExpose)
                {
                    _triggeredExpose = true;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _triggeredExpose = false;
                        DoPaint();
                    }, DispatcherPriority.Render);
                }
            }
            else if (ev.type == XEventName.FocusIn)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                Activated?.Invoke();
            }
            else if (ev.type == XEventName.FocusOut)
                Deactivated?.Invoke();
            else if (ev.type == XEventName.MotionNotify)
                MouseEvent(RawPointerEventType.Move, ref ev, ev.MotionEvent.state);
            else if (ev.type == XEventName.LeaveNotify)
                MouseEvent(RawPointerEventType.LeaveWindow, ref ev, ev.CrossingEvent.state);
            else if (ev.type == XEventName.PropertyNotify)
            {
                OnPropertyChange(ev.PropertyEvent.atom, ev.PropertyEvent.state == 0);
            }
            else if (ev.type == XEventName.ButtonPress)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                if (ev.ButtonEvent.button < 4)
                    MouseEvent(ev.ButtonEvent.button == 1 ? RawPointerEventType.LeftButtonDown
                        : ev.ButtonEvent.button == 2 ? RawPointerEventType.MiddleButtonDown
                        : RawPointerEventType.RightButtonDown, ref ev, ev.ButtonEvent.state);
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
                        TranslateModifiers(ev.ButtonEvent.state)), ref ev);
                }
                
            }
            else if (ev.type == XEventName.ButtonRelease)
            {
                if (ev.ButtonEvent.button < 4)
                    MouseEvent(ev.ButtonEvent.button == 1 ? RawPointerEventType.LeftButtonUp
                        : ev.ButtonEvent.button == 2 ? RawPointerEventType.MiddleButtonUp
                        : RawPointerEventType.RightButtonUp, ref ev, ev.ButtonEvent.state);
            }
            else if (ev.type == XEventName.ConfigureNotify)
            {
                if (ev.ConfigureEvent.window != _handle)
                    return;
                var needEnqueue = (_configure == null);
                _configure = ev.ConfigureEvent;
                if (ev.ConfigureEvent.override_redirect || ev.ConfigureEvent.send_event)
                    _configurePoint = new PixelPoint(ev.ConfigureEvent.x, ev.ConfigureEvent.y);
                else
                {
                    XTranslateCoordinates(_x11.Display, _handle, _x11.RootWindow,
                        0, 0,
                        out var tx, out var ty, out _);
                    _configurePoint = new PixelPoint(tx, ty);
                }
                if (needEnqueue)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_configure == null)
                            return;
                        var cev = _configure.Value;
                        var npos = _configurePoint.Value;
                        _configure = null;
                        _configurePoint = null;
                        
                        var nsize = new PixelSize(cev.width, cev.height);
                        var changedSize = _realSize != nsize;
                        var changedPos = _position == null || npos != _position;
                        _realSize = nsize;
                        _position = npos;
                        bool updatedSizeViaScaling = false;
                        if (changedPos)
                        {
                            PositionChanged?.Invoke(npos);
                            updatedSizeViaScaling = UpdateScaling();
                        }

                        if (changedSize && !updatedSizeViaScaling)
                            Resized?.Invoke(ClientSize);

                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Layout);
                    }, DispatcherPriority.Layout);
                if (_useRenderWindow)
                    XConfigureResizeWindow(_x11.Display, _renderHandle, ev.ConfigureEvent.width,
                        ev.ConfigureEvent.height);
            }
            else if (ev.type == XEventName.DestroyNotify && ev.AnyEvent.window == _handle)
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
            else if (ev.type == XEventName.KeyPress || ev.type == XEventName.KeyRelease)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                var buffer = stackalloc byte[40];

                var index = ev.KeyEvent.state.HasFlag(XModifierMask.ShiftMask);
                
                // We need the latin key, since it's mainly used for hotkeys, we use a different API for text anyway
                var key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 1 : 0).ToInt32();
                
                // Manually switch the Shift index for the keypad,
                // there should be a proper way to do this
                if (ev.KeyEvent.state.HasFlag(XModifierMask.Mod2Mask)
                    && key > X11Key.Num_Lock && key <= X11Key.KP_9)
                    key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 0 : 1).ToInt32();
                
                
                ScheduleInput(new RawKeyEventArgs(_keyboard, (ulong)ev.KeyEvent.time.ToInt64(),
                    ev.type == XEventName.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                    X11KeyTransform.ConvertKey(key), TranslateModifiers(ev.KeyEvent.state)), ref ev);

                if (ev.type == XEventName.KeyPress)
                {
                    var len = Xutf8LookupString(_xic, ref ev, buffer, 40, out _, out _);
                    if (len != 0)
                    {
                        var text = Encoding.UTF8.GetString(buffer, len);
                        if (text.Length == 1)
                        {
                            if (text[0] < ' ' || text[0] == 0x7f) //Control codes or DEL
                                return;
                        }
                        ScheduleInput(new RawTextInputEventArgs(_keyboard, (ulong)ev.KeyEvent.time.ToInt64(), text),
                            ref ev);
                    }
                }
            }
        }

        private bool UpdateScaling()
        {
            lock (SyncRoot)
            {
                var monitor = _platform.X11Screens.Screens.OrderBy(x => x.PixelDensity)
                    .FirstOrDefault(m => m.Bounds.Contains(Position));
                var newScaling = monitor?.PixelDensity ?? Scaling;
                if (Scaling != newScaling)
                {
                    Console.WriteLine(
                        $"Updating scaling from {Scaling} to {newScaling} as a response to position change to {Position}");
                    var oldScaledSize = ClientSize;
                    Scaling = newScaling;
                    ScalingChanged?.Invoke(Scaling);
                    SetMinMaxSize(_scaledMinMaxSize.minSize, _scaledMinMaxSize.maxSize);
                    Resize(oldScaledSize, true);
                    return true;
                }

                return false;
            }
        }

        private WindowState _lastWindowState;
        public WindowState WindowState
        {
            get => _lastWindowState;
            set
            {
                if(_lastWindowState == value)
                    return;
                _lastWindowState = value;
                if (value == WindowState.Minimized)
                {
                    XIconifyWindow(_x11.Display, _handle, _x11.DefaultScreen);
                }
                else if (value == WindowState.Maximized)
                {
                    SendNetWMMessage(_x11.Atoms._NET_WM_STATE, (IntPtr)0, _x11.Atoms._NET_WM_STATE_HIDDEN, IntPtr.Zero);
                    SendNetWMMessage(_x11.Atoms._NET_WM_STATE, (IntPtr)1, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
                else
                {
                    SendNetWMMessage(_x11.Atoms._NET_WM_STATE, (IntPtr)0, _x11.Atoms._NET_WM_STATE_HIDDEN, IntPtr.Zero);
                    SendNetWMMessage(_x11.Atoms._NET_WM_STATE, (IntPtr)0, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
            }
        }

        private void OnPropertyChange(IntPtr atom, bool hasValue)
        {
            if (atom == _x11.Atoms._NET_WM_STATE)
            {
                WindowState state = WindowState.Normal;
                if(hasValue)
                {

                    XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
                        false, (IntPtr)Atom.XA_ATOM, out _, out _, out var nitems, out _,
                        out var prop);
                    int maximized = 0;
                    var pitems = (IntPtr*)prop.ToPointer();
                    for (var c = 0; c < nitems.ToInt32(); c++)
                    {
                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_HIDDEN)
                        {
                            state = WindowState.Minimized;
                            break;
                        }

                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ ||
                            pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT)
                        {
                            maximized++;
                            if (maximized == 2)
                            {
                                state = WindowState.Maximized;
                                break;
                            }
                        }
                    }
                    XFree(prop);
                }
                if (_lastWindowState != state)
                {
                    _lastWindowState = state;
                    WindowStateChanged?.Invoke(state);
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
        private const int MaxWindowDimension = 100000;

        private (Size minSize, Size maxSize) _scaledMinMaxSize =
            (new Size(1, 1), new Size(double.PositiveInfinity, double.PositiveInfinity));

        private (PixelSize minSize, PixelSize maxSize) _minMaxSize = (new PixelSize(1, 1),
            new PixelSize(MaxWindowDimension, MaxWindowDimension));
        
        private double _scaling = 1;

        void ScheduleInput(RawInputEventArgs args, ref XEvent xev)
        {
            _x11.LastActivityTimestamp = xev.ButtonEvent.time;
            ScheduleInput(args);
        }

        public void ScheduleInput(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs mouse)
                mouse.Position = mouse.Position / Scaling;
            if (args is RawDragEvent drag)
                drag.Location = drag.Location / Scaling;
            
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
        
        void MouseEvent(RawPointerEventType type, ref XEvent ev, XModifierMask mods)
        {
            var mev = new RawPointerEventArgs(
                _mouse, (ulong)ev.ButtonEvent.time.ToInt64(), _inputRoot,
                type, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), TranslateModifiers(mods)); 
            if(type == RawPointerEventType.Move && _inputQueue.Count>0 && _lastEvent.Event is RawPointerEventArgs ma)
                if (ma.Type == RawPointerEventType.Move)
                {
                    _lastEvent.Event = mev;
                    return;
                }
            ScheduleInput(mev, ref ev);
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

        public IInputRoot InputRoot => _inputRoot;
        
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
            SetTransientParent(null, false);
            if (_xic != IntPtr.Zero)
            {
                XDestroyIC(_xic);
                _xic = IntPtr.Zero;
            }
            
            if (_handle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _handle);
                _platform.Windows.Remove(_handle);
                _platform.XI2?.OnWindowDestroyed(_handle);
                _handle = IntPtr.Zero;
                Closed?.Invoke();
            }
            
            if (_useRenderWindow && _renderHandle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _renderHandle);
                _renderHandle = IntPtr.Zero;
            }
        }

        bool ActivateTransientChildIfNeeded()
        {
            if (_transientChildren.Count == 0)
                return false;
            var child = _transientChildren.First();
            if (!child.ActivateTransientChildIfNeeded())
                child.Activate();
            return true;
        }
        
        void SetTransientParent(X11Window window, bool informServer = true)
        {
            _transientParent?._transientChildren.Remove(this);
            _transientParent = window;
            _transientParent?._transientChildren.Add(this);
            if (informServer)
                XSetTransientForHint(_x11.Display, _handle, _transientParent?._handle ?? IntPtr.Zero);
        }

        public void Show()
        {
            SetTransientParent(null);
            ShowCore();
        }
        
        void ShowCore()
        {
            XMapWindow(_x11.Display, _handle);
            XFlush(_x11.Display);
        }

        public void Hide() => XUnmapWindow(_x11.Display, _handle);
        
        
        public Point PointToClient(PixelPoint point) => new Point((point.X - Position.X) / Scaling, (point.Y - Position.Y) / Scaling);

        public PixelPoint PointToScreen(Point point) => new PixelPoint(
            (int)(point.X * Scaling + Position.X),
            (int)(point.Y * Scaling + Position.Y));
        
        public void SetSystemDecorations(bool enabled)
        {
            _systemDecorations = enabled;
            UpdateMotifHints();
        }


        public void Resize(Size clientSize) => Resize(clientSize, false);

        PixelSize ToPixelSize(Size size) => new PixelSize((int)(size.Width * Scaling), (int)(size.Height * Scaling));
        
        void Resize(Size clientSize, bool force)
        {
            if (!force && clientSize == ClientSize)
                return;
            
            var needImmediatePopupResize = clientSize != ClientSize;

            var pixelSize = ToPixelSize(clientSize);
            UpdateSizeHints(pixelSize);
            XConfigureResizeWindow(_x11.Display, _handle, pixelSize);
            if (_useRenderWindow)
                XConfigureResizeWindow(_x11.Display, _renderHandle, pixelSize);
            XFlush(_x11.Display);

            if (force || (_popup && needImmediatePopupResize))
            {
                _realSize = pixelSize;
                Resized?.Invoke(ClientSize);
            }
        }
        
        public void CanResize(bool value)
        {
            _canResize = value;
            UpdateMotifHints();
            UpdateSizeHints(null);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            if (cursor == null)
                XDefineCursor(_x11.Display, _handle, _x11.DefaultCursor);
            else
            {
                if (cursor.HandleDescriptor != "XCURSOR")
                    throw new ArgumentException("Expected XCURSOR handle type");
                XDefineCursor(_x11.Display, _handle, cursor.Handle);
            }
        }

        public IPlatformHandle Handle { get; }
        
        public PixelPoint Position
        {
            get => _position ?? default;
            set
            {
                var changes = new XWindowChanges
                {
                    x = (int)value.X,
                    y = (int)value.Y
                };
                XConfigureWindow(_x11.Display, _handle, ChangeWindowFlags.CWX | ChangeWindowFlags.CWY,
                    ref changes);
                XFlush(_x11.Display);

            }
        }

        public IMouseDevice MouseDevice => _mouse;
       
        public void Activate()
        {
            if (_x11.Atoms._NET_ACTIVE_WINDOW != IntPtr.Zero)
            {
                SendNetWMMessage(_x11.Atoms._NET_ACTIVE_WINDOW, (IntPtr)1, _x11.LastActivityTimestamp,
                    IntPtr.Zero);
            }
            else
            {
                XRaiseWindow(_x11.Display, _handle);
                XSetInputFocus(_x11.Display, _handle, 0, IntPtr.Zero);
            }
        }


        public IScreenImpl Screen => _platform.Screens;

        public Size MaxClientSize => _platform.X11Screens.Screens.Select(s => s.Bounds.Size.ToSize(s.PixelDensity))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();


        void SendNetWMMessage(IntPtr message_type, IntPtr l0,
            IntPtr? l1 = null, IntPtr? l2 = null, IntPtr? l3 = null, IntPtr? l4 = null)
        {
            var xev = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    send_event = true,
                    window = _handle,
                    message_type = message_type,
                    format = 32,
                    ptr1 = l0,
                    ptr2 = l1 ?? IntPtr.Zero,
                    ptr3 = l2 ?? IntPtr.Zero,
                    ptr4 = l3 ?? IntPtr.Zero
                }
            };
            xev.ClientMessageEvent.ptr4 = l4 ?? IntPtr.Zero;
            XSendEvent(_x11.Display, _x11.RootWindow, false,
                new IntPtr((int)(EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);

        }

        void BeginMoveResize(NetWmMoveResize side)
        {
            var pos = GetCursorPos(_x11);
            XUngrabPointer(_x11.Display, new IntPtr(0));
            SendNetWMMessage (_x11.Atoms._NET_WM_MOVERESIZE, (IntPtr) pos.x, (IntPtr) pos.y,
                (IntPtr) side,
                (IntPtr) 1, (IntPtr)1); // left button
        }

        public void BeginMoveDrag()
        {
            BeginMoveResize(NetWmMoveResize._NET_WM_MOVERESIZE_MOVE);
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            var side = NetWmMoveResize._NET_WM_MOVERESIZE_CANCEL;
            if (edge == WindowEdge.East)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_RIGHT;
            if (edge == WindowEdge.North)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOP;
            if (edge == WindowEdge.South)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOM;
            if (edge == WindowEdge.West)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_LEFT;
            if (edge == WindowEdge.NorthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPRIGHT;
            if (edge == WindowEdge.NorthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPLEFT;
            if (edge == WindowEdge.SouthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMRIGHT;
            if (edge == WindowEdge.SouthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMLEFT;
            BeginMoveResize(side);
        }
        
        public void SetTitle(string title)
        {
            var data = Encoding.UTF8.GetBytes(title);
            fixed (void* pdata = data)
            {
                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_NAME, _x11.Atoms.UTF8_STRING, 8,
                    PropertyMode.Replace, pdata, data.Length);
                XStoreName(_x11.Display, _handle, title);
            }
        }

        public void SetWmClass(string wmClass)
        {
            var data = Encoding.ASCII.GetBytes(wmClass);
            fixed (void* pdata = data)
            {
                XChangeProperty(_x11.Display, _handle, _x11.Atoms.XA_WM_CLASS, _x11.Atoms.XA_STRING, 8,
                    PropertyMode.Replace, pdata, data.Length);
            }
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _scaledMinMaxSize = (minSize, maxSize);
            var min = new PixelSize(
                (int)(minSize.Width < 1 ? 1 : minSize.Width * Scaling),
                (int)(minSize.Height < 1 ? 1 : minSize.Height * Scaling));

            const int maxDim = MaxWindowDimension;
            var max = new PixelSize(
                (int)(maxSize.Width > maxDim ? maxDim : Math.Max(min.Width, maxSize.Width * Scaling)),
                (int)(maxSize.Height > maxDim ? maxDim : Math.Max(min.Height, maxSize.Height * Scaling)));
            
            _minMaxSize = (min, max);
            UpdateSizeHints(null);
        }

        public void SetTopmost(bool value)
        {
            SendNetWMMessage(_x11.Atoms._NET_WM_STATE,
                (IntPtr)(value ? 1 : 0), _x11.Atoms._NET_WM_STATE_ABOVE, IntPtr.Zero);
        }

        public void ShowDialog(IWindowImpl parent)
        {
            SetTransientParent((X11Window)parent);
            ShowCore();
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            var data = ((X11IconData)icon).Data;
            fixed (void* pdata = data)
                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_ICON,
                    new IntPtr((int)Atom.XA_CARDINAL), 32, PropertyMode.Replace,
                    pdata, data.Length);
        }

        public void ShowTaskbarIcon(bool value)
        {
            SendNetWMMessage(_x11.Atoms._NET_WM_STATE,
                (IntPtr)(value ? 0 : 1), _x11.Atoms._NET_WM_STATE_SKIP_TASKBAR, IntPtr.Zero);
        }        
    }
}
