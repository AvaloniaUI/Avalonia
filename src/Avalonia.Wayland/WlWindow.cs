using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Wayland.Egl;
using Avalonia.Wayland.Framebuffer;
using NWayland.Protocols.FractionalScaleV1;
using NWayland.Protocols.Plasma.Blur;
using NWayland.Protocols.Viewporter;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland
{
    internal abstract class WlWindow : IWindowBaseImpl, WlSurface.IEvents, WlCallback.IEvents, XdgSurface.IEvents, WpFractionalScaleV1.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlFramebufferSurface _wlFramebufferSurface;
        private readonly WlEglGlPlatformSurface? _wlEglGlPlatformSurface;
        private readonly WpViewport? _wpViewport;
        private readonly WpFractionalScaleV1? _wpFractionalScale;
        private readonly object _resizeLock = new();

        private bool _didResize;
        private WlCallback? _frameCallback;
        private OrgKdeKwinBlur? _blur;

        internal bool DidReceiveInitialConfigure;
        internal State PendingState;
        internal State AppliedState;

        internal struct State
        {
            public uint ConfigureSerial;
            public PixelSize Size;
            public PixelSize Bounds;
            public WindowState WindowState;
            public bool NeedsWindowDecoration;
            public bool Activated;
        }

        protected WlWindow(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            WlSurface = platform.WlCompositor.CreateSurface();
            XdgSurface = platform.XdgWmBase.GetXdgSurface(WlSurface);
            XdgSurface.Events = this;

            if (platform.WpViewporter is not null && platform.WpFractionalScaleManager is not null)
            {
                _wpViewport = platform.WpViewporter.GetViewport(WlSurface);
                _wpFractionalScale = platform.WpFractionalScaleManager.GetFractionalScale(WlSurface);
                _wpFractionalScale.Events = this;
            }

            var surfaces = new List<object>(2);

            var platformGraphics = AvaloniaLocator.Current.GetService<IPlatformGraphics>();
            if (platformGraphics is EglPlatformGraphics)
            {
                var surfaceInfo = new WlEglSurfaceInfo(this);
                _wlEglGlPlatformSurface = new WlEglGlPlatformSurface(surfaceInfo);
                surfaces.Add(_wlEglGlPlatformSurface);
            }

            _wlFramebufferSurface = new WlFramebufferSurface(_platform, this);
            surfaces.Add(_wlFramebufferSurface);

            Surfaces = surfaces.ToArray();

            platform.WlScreens.AddWindow(this);
        }

        public IPlatformHandle Handle => null!;

        public Size MaxAutoSizeHint => AppliedState.Bounds != default ? AppliedState.Bounds.ToSize(RenderScaling) : Size.Infinity;

        public Size ClientSize => AppliedState.Size.ToSize(RenderScaling);

        public Size? FrameSize => null;

        public PixelPoint Position { get; protected set; }

        public double RenderScaling { get; private set; } = 1;

        public double DesktopScaling => RenderScaling;

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => default;

        public IScreenImpl Screen => _platform.WlScreens;

        public IEnumerable<object> Surfaces { get; }

        public Action<RawInputEventArgs>? Input { get; set; }

        public Action<Rect>? Paint { get; set; }

        public Action<Size, WindowResizeReason>? Resized { get; set; }

        public Action<double>? ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public Action? Activated { get; set; }

        public Action? Deactivated { get; set; }

        public Action? LostFocus { get; set; }

        public Action? Closed { get; set; }

        public Action<PixelPoint>? PositionChanged { get; set; }

        internal IInputRoot? InputRoot { get; private set; }

        internal WlWindow? Parent { get; set; }

        internal WlSurface WlSurface { get; }

        internal XdgSurface XdgSurface { get; }

        protected WlOutput? WlOutput { get; private set; }

        public IRenderer CreateRenderer(IRenderRoot root) => new CompositingRenderer(root, _platform.Compositor, () => Surfaces);

        public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

        public Point PointToClient(PixelPoint point) => new((point.X - Position.X) / RenderScaling, (point.Y - Position.Y) / RenderScaling);

        public PixelPoint PointToScreen(Point point) => new((int)(point.X * RenderScaling + Position.X), (int)(point.Y * RenderScaling + Position.Y));

        public void SetCursor(ICursorImpl? cursor) => _platform.WlInputDevice.PointerHandler?.SetCursor(cursor as WlCursor);

        public IPopupImpl CreatePopup() => new WlPopup(_platform, this);

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            if (transparencyLevel == TransparencyLevel)
                return;
            TransparencyLevel = transparencyLevel;
            ApplyTransparencyLevel(transparencyLevel);
            TransparencyLevelChanged?.Invoke(transparencyLevel);
        }

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant) { }

        public virtual void Show(bool activate, bool isDialog)
        {
            WlSurface.Commit();
            _platform.WlDisplay.Roundtrip();
        }

        public virtual void Hide()
        {
            WlSurface.Attach(null, 0, 0);
            WlSurface.Commit();
            _platform.WlDisplay.Roundtrip();
        }

        public void Activate() { }

        public void SetTopmost(bool value) { } // impossible on Wayland

        public void Resize(Size clientSize, WindowResizeReason reason = WindowResizeReason.Application)
        {
            if (!DidReceiveInitialConfigure && clientSize != default)
            {
                PendingState.Size = PixelSize.FromSize(clientSize, RenderScaling);
                _didResize = true;
            }
        }

        public void OnDone(WlCallback eventSender, uint callbackData)
        {
            _frameCallback!.Dispose();
            _frameCallback = null;
            Dispatcher.UIThread.Post(DoPaint, DispatcherPriority.Render);
        }

        public void OnEnter(WlSurface eventSender, WlOutput output)
        {
            WlOutput = output;
        }

        public void OnLeave(WlSurface eventSender, WlOutput output)
        {
            if (WlOutput == output)
                WlOutput = null;
        }

        public void OnPreferredBufferScale(WlSurface eventSender, int factor) { }

        public void OnPreferredBufferTransform(WlSurface eventSender, WlOutput.TransformEnum transform) { }

        public abstract object? TryGetFeature(Type featureType);

        public void OnConfigure(XdgSurface eventSender, uint serial)
        {
            if (AppliedState.ConfigureSerial == serial)
                return;

            PendingState.ConfigureSerial = serial;

            lock (_resizeLock)
            {
                ApplyConfigure();
                XdgSurface.AckConfigure(serial);
            }
        }

        public void OnPreferredScale(WpFractionalScaleV1 eventSender, uint scale)
        {
            RenderScaling = scale / 120d;
            if (AppliedState.Size != default)
                _wpViewport!.SetDestination(AppliedState.Size.Width, AppliedState.Size.Height);
            ScalingChanged?.Invoke(RenderScaling);
        }

        public virtual void Dispose()
        {
            _platform.WlScreens.RemoveWindow(this);
            _blur?.Dispose();
            _frameCallback?.Dispose();
            _wlFramebufferSurface.Dispose();
            _wlEglGlPlatformSurface?.Dispose();
            _wpFractionalScale?.Dispose();
            XdgSurface.Dispose();
            WlSurface.Dispose();
            Closed?.Invoke();
        }

        internal void RequestFrame()
        {
            if (_frameCallback is not null)
                return;
            DoPaint();
            _frameCallback = WlSurface.Frame();
            _frameCallback.Events = this;
        }

        protected virtual void ApplyConfigure()
        {
            if (AppliedState.Size != PendingState.Size)
                _didResize = true;

            AppliedState = PendingState;

            if (!DidReceiveInitialConfigure)
            {
                // Emulate Window 7+'s default window size behavior in case no explicit size was set. If no configure_bounds event was send, fall back to a hardcoded size.
                if (AppliedState.Size == default)
                    AppliedState.Size = new PixelSize(Math.Max((int)(AppliedState.Bounds.Width * 0.75), 300), Math.Max((int)(AppliedState.Bounds.Height * 0.7), 200));

                DidReceiveInitialConfigure = true;
                DoPaint();
            }
            else if (_didResize)
            {
                RequestFrame();
                WlSurface.Commit();
            }
        }

        private void DoPaint()
        {
            lock (_resizeLock)
            {
                if (_didResize)
                {
                    Resized?.Invoke(ClientSize, WindowResizeReason.Application);
                    ApplyTransparencyLevel(TransparencyLevel);
                    _didResize = false;
                }

                Paint?.Invoke(default);
            }
        }

        private void ApplyTransparencyLevel(WindowTransparencyLevel transparencyLevel)
        {
            switch (transparencyLevel)
            {
                case WindowTransparencyLevel.None:
                {
                    _platform.KdeKwinBlurManager?.Unset(WlSurface);
                    using var region = _platform.WlCompositor.CreateRegion();
                    region.Add(0, 0, AppliedState.Size.Width, AppliedState.Size.Height);
                    WlSurface.SetOpaqueRegion(region);
                    break;
                }
                case WindowTransparencyLevel.Transparent:
                    _platform.KdeKwinBlurManager?.Unset(WlSurface);
                    WlSurface.SetOpaqueRegion(null);
                    break;
                case >= WindowTransparencyLevel.Blur when _platform.KdeKwinBlurManager is not null:
                {
                    _blur?.Dispose();
                    WlSurface.SetOpaqueRegion(null);
                    using var region = _platform.WlCompositor.CreateRegion();
                    region.Add(0, 0, AppliedState.Size.Width, AppliedState.Size.Height);
                    _blur = _platform.KdeKwinBlurManager.Create(WlSurface);
                    _blur.SetRegion(region);
                    _blur.Commit();
                    break;
                }
            }
        }
    }
}
