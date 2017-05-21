using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Our infrastructure is currently tries to use toplevel impl even if
    ///  it already have been disposed. To prevent sudden crashes, as a
    ///  duct tape measure an instance of this class is used instead of an
    ///  actual impl
    /// </summary>
    class DisposedTopLevelImpl : IWindowImpl, IEmbeddableWindowImpl
    {
        public void Dispose()
        {
            
        }

        void Log([CallerMemberName]string caller = null)
        {
            Avalonia.Logging.Logger.Warning(LogArea.Control, this, "Call to disposed topleve impl {0}", caller);
        }

        public Size ClientSize
        {
            get
            {
                Log();
                return new Size(0, 0);
            }
        }

        public double Scaling
        {
            get
            {
                Log();
                return 1;
            }
        }

        public IEnumerable<object> Surfaces
        {
            get
            {
                Log();
                return new object[0];
            }
        }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public void Invalidate(Rect rect) => Log();

        public void SetInputRoot(IInputRoot inputRoot) => Log();

        public Point PointToClient(Point point)
        {
            Log();
            return point;
        }

        public Point PointToScreen(Point point)
        {
            Log();
            return point;
        }

        public void SetCursor(IPlatformHandle cursor) => Log();

        public Action Closed { get; set; }

        public void Show() => Log();

        public void Hide() => Log();

        public void BeginMoveDrag() => Log();

        public void BeginResizeDrag(WindowEdge edge) => Log();

        public Point Position
        {
            get
            {
                Log();
                return default(Point);
            }
            set { Log(); }
        }

        public Action<Point> PositionChanged { get; set; }

        public void Activate() => Log();

        public Action Deactivated { get; set; }

        public Action Activated { get; set; }

        public IPlatformHandle Handle
        {
            get
            {
                Log();
                return null;
            }
        }

        public Size MaxClientSize
        {
            get
            {
                Log();
                return new Size(0, 0);
            }
        }

        public void Resize(Size clientSize) => Log();

        public WindowState WindowState
        {
            get
            {
                Log();
                return WindowState.Normal;
            }
            set { Log(); }
        }

        public void SetTitle(string title) => Log();

        public IDisposable ShowDialog()
        {
            Log();
            return Disposable.Empty;
        }

        public void SetSystemDecorations(bool enabled) => Log();

        public void SetIcon(IWindowIconImpl icon) => Log();

        public event Action LostFocus;
    }
}
