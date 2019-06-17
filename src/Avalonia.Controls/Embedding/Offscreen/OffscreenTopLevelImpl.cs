using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Controls.Embedding.Offscreen
{
    public abstract class OffscreenTopLevelImplBase : ITopLevelImpl
    {
        private double _scaling = 1;
        private Size _clientSize;

        public IInputRoot InputRoot { get; private set; }
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public IRenderer CreateRenderer(IRenderRoot root) => new ImmediateRenderer(root);

        public abstract void Invalidate(Rect rect);
        public abstract IEnumerable<object> Surfaces { get; }

        public Size ClientSize
        {
            get { return _clientSize; }
            set
            {
                _clientSize = value;
                Resized?.Invoke(value);
            }
        }

        public double Scaling
        {
            get { return _scaling; }
            set
            {
                _scaling = value;
                ScalingChanged?.Invoke(value);
            }
        }
        
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

        public virtual Point PointToClient(PixelPoint point) => point.ToPoint(1);

        public virtual PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, 1);

        public virtual void SetCursor(IPlatformHandle cursor)
        {
        }

        public Action Closed { get; set; }
        public abstract IMouseDevice MouseDevice { get; }
        public abstract IKeyboardDevice KeyboardDevice { get; }
    }
}
