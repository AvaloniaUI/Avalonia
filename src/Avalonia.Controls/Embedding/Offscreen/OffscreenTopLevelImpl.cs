using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Controls.Embedding.Offscreen
{
    [Unstable]
    public abstract class OffscreenTopLevelImplBase : ITopLevelImpl
    {
        private double _scaling = 1;
        private Size _clientSize;
        private ManualRenderTimer _manualRenderTimer = new();

        public IInputRoot? InputRoot { get; private set; }
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        class ManualRenderTimer : IRenderTimer
        {
            static Stopwatch St = Stopwatch.StartNew(); 
            public event Action<TimeSpan>? Tick;
            public bool RunsInBackground => false;
            public void TriggerTick() => Tick?.Invoke(St.Elapsed);
        }

        public Compositor Compositor { get; }

        public OffscreenTopLevelImplBase()
        {
            Compositor = new Compositor(new RenderLoop(_manualRenderTimer), null, false,
                MediaContext.Instance, false);
        }

        public abstract IEnumerable<object> Surfaces { get; }

        public Size ClientSize
        {
            get { return _clientSize; }
            set
            {
                _clientSize = value;
                Resized?.Invoke(value, WindowResizeReason.Unspecified);
            }
        }

        public Size? FrameSize => null;

        public double RenderScaling
        {
            get { return _scaling; }
            set
            {
                _scaling = value;
                ScalingChanged?.Invoke(value);
            }
        }
        
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant) { }

        /// <inheritdoc/>
        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);

        public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

        public virtual Point PointToClient(PixelPoint point) => point.ToPoint(1);

        public virtual PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, 1);

        public virtual void SetCursor(ICursorImpl? cursor)
        {
        }

        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public abstract IMouseDevice MouseDevice { get; }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel) { }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public IPopupImpl? CreatePopup() => null;
        
        public virtual object? TryGetFeature(Type featureType) => null;
    }
}
