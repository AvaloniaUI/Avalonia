using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Controls.Embedding.Offscreen
{
    abstract class OffscreenTopLevelImplBase : ITopLevelImpl
    {
        private double _scaling;
        private Size _clientSize;
        public IInputRoot InputRoot { get; private set; }

        public virtual void Dispose()
        {
            //No-op
        }

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

        public virtual Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public virtual void SetCursor(IPlatformHandle cursor)
        {
        }

        public Action Closed { get; set; }
    }
}
