using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    abstract class TopLevelImpl : ITopLevelImpl, IPlatformHandle
    {
        protected readonly IntPtr GtkWidget;
        private IInputRoot _inputRoot;
        protected readonly List<IDisposable> _disposables = new List<IDisposable>();

        public TopLevelImpl(IntPtr gtkWidget)
        {
            GtkWidget = gtkWidget;
            Native.GtkWidgetSetEvents(gtkWidget, uint.MaxValue);
            Native.GtkWidgetRealize(gtkWidget);
            Connect<Native.D.signal_widget_draw>("draw", OnDraw);
            Connect<Native.D.signal_onevent>("configure-event", OnConfigured);
        }

        private bool OnConfigured(IntPtr gtkwidget, IntPtr ev, IntPtr userdata)
        {
            Debug.WriteLine("Configured");
            Resized?.Invoke(ClientSize);
            return false;
        }


        void Connect<T>(string name, T handler) => _disposables.Add(Signal.Connect<T>(GtkWidget, name, handler));

        private bool OnDraw(IntPtr gtkwidget, IntPtr cairocontext, IntPtr userdata)
        {
            Debug.WriteLine("Draw");
            Paint?.Invoke(new Rect(ClientSize));
            return true;
        }

        public void Dispose()
        {
            foreach(var d in _disposables)
                d.Dispose();
            _disposables.Clear();
            //TODO
        }

        public abstract Size ClientSize { get; set; }

        public Size MaxClientSize
        {
            get
            {
                var s = Native.GtkWidgetGetScreen(GtkWidget);
                return new Size(Native.GdkScreenGetWidth(s), Native.GdkScreenGetHeight(s));
            }
        }


        public double Scaling => 1; //TODO: Implement scaling
        public IPlatformHandle Handle => this;

        string IPlatformHandle.HandleDescriptor => "HWND";

        public Action Activated { get; set; } //TODO
        public Action Closed { get; set; } //TODO
        public Action Deactivated { get; set; } //TODO
        public Action<RawInputEventArgs> Input { get; set; } //TODO
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; } //TODO
        public Action<double> ScalingChanged { get; set; } //TODO
        public Action<Point> PositionChanged { get; set; } //TODO
        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            Native.GtkWidgetQueueDrawArea(GtkWidget, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
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

        public void Show() => Native.GtkWindowPresent(GtkWidget);

        public void Hide() => Native.GtkWidgetHide(GtkWidget);

        public void BeginMoveDrag()
        {
            //STUB
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //STUB
        }

        public Point Position { get; set; }

        IntPtr IPlatformHandle.Handle => Native.GetNativeGdkWindowHandle(Native.GtkWidgetGetWindow(GtkWidget));
        public IEnumerable<object> Surfaces => new object[] {Handle};
    }
}
