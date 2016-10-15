using System;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    abstract class TopLevelImpl : ITopLevelImpl, IPlatformHandle
    {
        protected readonly IntPtr _gtkWidget;
        private IInputRoot _inputRoot;

        public TopLevelImpl(IntPtr gtkWidget)
        {
            _gtkWidget = gtkWidget;
            Native.GtkWidgetRealize(gtkWidget);
            Native.GtkWidgetSetDoubleBuffered(gtkWidget, false);
            Signal.Connect<Native.D.signal_widget_draw>(_gtkWidget, "draw", OnDraw);
        }

        private bool OnDraw(IntPtr gtkwidget, IntPtr cairocontext, IntPtr userdata)
        {
            Paint?.Invoke(new Rect(ClientSize));
            return true;
        }

        public void Dispose()
        {
            //STUB
        }

        public abstract Size ClientSize { get; set; }

        public Size MaxClientSize => new Size(1024, 768); //STUB
        public double Scaling => 1; //TODO: Implement scaling
        public IPlatformHandle Handle => this;

        string IPlatformHandle.HandleDescriptor => "HWND";

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action<Point> PositionChanged { get; set; }
        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

        public Point PointToClient(Point point)
        {
            throw new NotImplementedException();
        }

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            //STUB
        }

        public void Show() => Native.GtkWindowPresent(_gtkWidget);

        public void Hide() => Native.GtkWidgetHide(_gtkWidget);

        public void BeginMoveDrag()
        {
            //STUB
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //STUB
        }

        public Point Position { get; set; }

        IntPtr IPlatformHandle.Handle => Native.GetNativeGdkWindowHandle(Native.GtkWidgetGetWindow(_gtkWidget));
    }
}
