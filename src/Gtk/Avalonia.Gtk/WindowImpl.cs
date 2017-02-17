using System;
using System.Reactive.Disposables;
using Avalonia.Platform;
using Gdk;

namespace Avalonia.Gtk
{
    using Gtk = global::Gtk;
    public class WindowImpl : WindowImplBase
    {
        private Gtk.Window _window;
        private Gtk.Window Window => _window ?? (_window = (Gtk.Window) Widget);
        
        public WindowImpl(Gtk.WindowType type) : base(new PlatformHandleAwareWindow(type))
        {
            Init();
        }

        public WindowImpl()
            : base(new PlatformHandleAwareWindow(Gtk.WindowType.Toplevel) {DefaultSize = new Gdk.Size(900, 480)})
        {
            Init();
        }

        void Init()
        {
            Window.FocusActivated += OnFocusActivated;
            Window.ConfigureEvent += OnConfigureEvent;
            _lastClientSize = ClientSize;
            _lastPosition = Position;
        }
        private Size _lastClientSize;
        private Point _lastPosition;
        void OnConfigureEvent(object o, Gtk.ConfigureEventArgs args)
        {
            var evnt = args.Event;
            args.RetVal = true;
            var newSize = new Size(evnt.Width, evnt.Height);

            if (newSize != _lastClientSize)
            {
                Resized(newSize);
                _lastClientSize = newSize;
            }

            var newPosition = new Point(evnt.X, evnt.Y);
            
            if (newPosition != _lastPosition)
            {
                PositionChanged(newPosition);
                _lastPosition = newPosition;
            }
        }

        public override Size ClientSize
        {
            get
            {
                int width;
                int height;
                Window.GetSize(out width, out height);
                return new Size(width, height);
            }
        }

        public override void Resize(Size value)
        {
            Window.Resize((int)value.Width, (int)value.Height);
        }

        public override void SetTitle(string title)
        {
            Window.Title = title;
        }

        void OnFocusActivated(object sender, EventArgs eventArgs)
        {
            Activated();
        }

        public override void BeginMoveDrag()
        {
            int x, y;
            ModifierType mod;
            Window.Screen.RootWindow.GetPointer(out x, out y, out mod);
            Window.BeginMoveDrag(1, x, y, 0);
        }

        public override void BeginResizeDrag(Controls.WindowEdge edge)
        {
            int x, y;
            ModifierType mod;
            Window.Screen.RootWindow.GetPointer(out x, out y, out mod);
            Window.BeginResizeDrag((Gdk.WindowEdge)(int)edge, 1, x, y, 0);
        }

        public override Point Position
        {
            get
            {
                int x, y;
                Window.GetPosition(out x, out y);
                return new Point(x, y);
            }
            set
            {
                Window.Move((int)value.X, (int)value.Y);
            }
        }

        public override IDisposable ShowDialog()
        {
            Window.Modal = true;
            Window.Show();

            return Disposable.Empty;
        }

        public override void SetSystemDecorations(bool enabled) => Window.Decorated = enabled;

        public override void SetIcon(IWindowIconImpl icon)
        {
            Window.Icon = ((IconImpl)icon).Pixbuf;
        }
    }
}
