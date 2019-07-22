using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.LinuxFramebuffer
{
    class FramebufferToplevelImpl : IEmbeddableWindowImpl, IScreenInfoProvider
    {
        private readonly LinuxFramebuffer _fb;
        private readonly IInputBackend _inputBackend;
        private bool _renderQueued;
        public IInputRoot InputRoot { get; private set; }

        public FramebufferToplevelImpl(LinuxFramebuffer fb, IInputBackend inputBackend)
        {
            _fb = fb;
            _inputBackend = inputBackend;
            Invalidate(default(Rect));
            _inputBackend.Initialize(this, e => Input?.Invoke(e));
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());
        }

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        
        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
            _inputBackend.SetInputRoot(inputRoot);
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public Size ClientSize => _fb.PixelSize;
        public IMouseDevice MouseDevice => new MouseDevice();
        public double Scaling => 1;
        public IEnumerable<object> Surfaces => new object[] {_fb};
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action Closed { get; set; }
        public event Action LostFocus
        {
            add {}
            remove {}
        }

        public Size ScaledSize => _fb.PixelSize / Scaling;
    }
}
