using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Gtk3
{
    public static class EventManager
    {
        private static List<WindowBaseImpl> windowStack = new List<WindowBaseImpl>();
        private static List<WindowBaseImpl> modalStack = new List<WindowBaseImpl>();

        internal static IDisposable ConnectEvents(WindowBaseImpl impl)
        {
            var subscription = new EventSubscription(impl.GtkWidget);
            var userData = impl.GtkWidget.DangerousGetHandle();

            subscription.Connect<Native.D.signal_widget_draw>("draw", OnDraw, userData);
            subscription.Connect<Native.D.signal_generic>("realize", OnRealized, userData);
            subscription.Connect<Native.D.signal_generic>("destroy", OnDestroy, userData);
            subscription.Connect<Native.D.signal_generic>("show", OnShown, userData);

            subscription.ConnectEvent("configure-event", OnConfigured);
            subscription.ConnectEvent("button-press-event", OnButton);
            subscription.ConnectEvent("button-release-event", OnButton);
            subscription.ConnectEvent("motion-notify-event", OnMotion);
            subscription.ConnectEvent("scroll-event", OnScroll);
            subscription.ConnectEvent("window-state-event", OnStateChanged);
            subscription.ConnectEvent("key-press-event", OnKeyEvent);
            subscription.ConnectEvent("key-release-event", OnKeyEvent);
            subscription.ConnectEvent("leave-notify-event", OnLeaveNotifyEvent);
            subscription.ConnectEvent("delete-event", OnClosingEvent);

            if (Gtk3Platform.UseDeferredRendering)
            {
                Native.GtkWidgetSetDoubleBuffered(impl.GtkWidget, false);
                subscription.AddTickCallback(userData);
            }

            windowStack.Add(impl);

            if (!modalStack.Any())
                modalStack.Add(impl);
            else
            {
                var lastModal = modalStack.Last();
                if (!lastModal.GtkWidget.IsInvalid)
                    Native.GtkWindowSetTransientFor(impl.GtkWidget, lastModal.GtkWidget);
            }

            return subscription;
        }

        private static void OnShown(IntPtr gtkWidget, IntPtr userData)
        {
            var impl = GetImplementationFromGtkWindow(userData, true);

            if (impl != null)
            {
                // move it to front to be able to receive modal events.
                windowStack.Remove(impl);
                windowStack.Add(impl);
            }
        }

        private static void OnDestroy(IntPtr gtkWidget, IntPtr userData)
        {
            var impl = GetImplementationFromGtkWindow(userData);

            if (impl != null)
            {
                impl.DoDispose(true);
            }

            // this is potentially dangerous! either assert here or at least log an error
        }

        private static void OnRealized(IntPtr gtkWidget, IntPtr userData)
        {
            var impl = GetImplementationFromGtkWindow(userData);

            if (impl != null)
            {
                impl.OnRealized();
            }
        }

        private static bool OnDraw(IntPtr gtkWidget, IntPtr cairoContext, IntPtr userData)
        {
            var impl = GetImplementationFromGtkWindow(userData, true);

            if (impl == null)
            {
                Logger.Warning("EventManager", gtkWidget, "No window to route the OnDraw event to");
                return false;
            }

            impl.OnDraw(cairoContext);

            return true;
        }

        private static InputModifiers GetModifierKeys(GdkModifierType state)
        {
            var rv = InputModifiers.None;
            if (state.HasFlag(GdkModifierType.ControlMask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(GdkModifierType.ShiftMask))
                rv |= InputModifiers.Shift;
            if (state.HasFlag(GdkModifierType.Mod1Mask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(GdkModifierType.Button1Mask))
                rv |= InputModifiers.LeftMouseButton;
            if (state.HasFlag(GdkModifierType.Button2Mask))
                rv |= InputModifiers.RightMouseButton;
            if (state.HasFlag(GdkModifierType.Button3Mask))
                rv |= InputModifiers.MiddleMouseButton;
            return rv;
        }

        private static WindowBaseImpl GetImplementationFromEvent(IntPtr @event, bool ignoreModal = false)
        {
            var gdkWidget = Native.GdkEventGetWindow(@event);
            var topLevel = Native.GdkWindowGetTopLevel(gdkWidget);

            var impl = windowStack.FirstOrDefault(wbi => Native.GtkWidgetGetWindow(wbi.GtkWidget) == topLevel);

            return FilterImplementationThroughModal(impl, ignoreModal);
        }

        private static WindowBaseImpl GetImplementationFromGtkWindow(IntPtr gtkWindow, bool ignoreModal = false)
        {
            var impl = windowStack.FirstOrDefault(wbi => wbi.GtkWidget.DangerousGetHandle() == gtkWindow);
            return FilterImplementationThroughModal(impl, ignoreModal);
        }

        private static WindowBaseImpl FilterImplementationThroughModal(WindowBaseImpl impl, bool ignoreModal = false)
        {
            var index = windowStack.IndexOf(impl);
            if (modalStack.Any())
            {
                var modal = modalStack.LastOrDefault();
                var modalIndex = windowStack.IndexOf(modal);
                if (modalIndex > index && !ignoreModal)
                    return null;
            }

            return impl;
        }

        private static bool OnClosingEvent(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev);

            if (impl == null)
            {
                return true;
            }

            bool? preventClosing = impl.Closing?.Invoke();
            return preventClosing ?? false;
        }

        unsafe private static bool OnLeaveNotifyEvent(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev);

            if (impl == null)
                return false;

            var evnt = (GdkEventCrossing*)ev;
            var position = new Point(evnt->x, evnt->y);
            impl.OnInput(new RawMouseEventArgs(Gtk3Platform.Mouse,
                evnt->time,
                impl.GetInputRoot(),
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state)));

            return true;
        }

        unsafe private static bool OnKeyEvent(IntPtr gtkWidget, IntPtr pev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(pev);

            if (impl == null)
                return false;

            var evnt = (GdkEventKey*)pev;
            if (impl.FilterKeypress(evnt))
                return true;
           
            var e = new RawKeyEventArgs(
                Gtk3Platform.Keyboard,
                evnt->time,
                evnt->type == GdkEventType.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                Avalonia.Gtk.Common.KeyTransform.ConvertKey((GdkKey)evnt->keyval), GetModifierKeys((GdkModifierType)evnt->state));
            impl.OnInput(e);
            return true;
        }

        unsafe private static bool OnStateChanged(IntPtr gtkWidget, IntPtr pev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(pev, true);

            if (impl == null)
                return false;

            var ev = (GdkEventWindowState*)pev;

            impl.OnStateChanged(ev->changed_mask, ev->new_window_state);

            return true;
        }

        unsafe private static bool OnScroll(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev);

            if (impl == null)
                return false;

            var evnt = (GdkEventScroll*)ev;

            //Ignore duplicates
            if (impl.FilterScroll(evnt->time, evnt->direction))
                return true;
            
            var delta = new Vector();
            const double step = (double)1;
            if (evnt->direction == GdkScrollDirection.Down)
                delta = new Vector(0, -step);
            else if (evnt->direction == GdkScrollDirection.Up)
                delta = new Vector(0, step);
            else if (evnt->direction == GdkScrollDirection.Right)
                delta = new Vector(-step, 0);
            else if (evnt->direction == GdkScrollDirection.Left)
                delta = new Vector(step, 0);
            else if (evnt->direction == GdkScrollDirection.Smooth)
                delta = new Vector(-evnt->delta_x, -evnt->delta_y);

            var e = new RawMouseWheelEventArgs(Gtk3Platform.Mouse, evnt->time,
                impl.GetInputRoot(),
                new Point(evnt->x, evnt->y), delta, GetModifierKeys(evnt->state));
            impl.OnInput(e);
            return true;
        }

        unsafe private static bool OnMotion(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev);

            if (impl == null)
                return false;

            var evnt = (GdkEventMotion*)ev;
            var position = new Point(evnt->x, evnt->y);
            Native.GdkEventRequestMotions(ev);
            var e = new RawMouseEventArgs(
                Gtk3Platform.Mouse,
                evnt->time,
                impl.GetInputRoot(),
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state));
            impl.OnInput(e);

            return true;
        }

        unsafe private static bool OnButton(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev);

            if (impl == null)
                return false;

            var evnt = (GdkEventButton*)ev;
            var e = new RawMouseEventArgs(
                Gtk3Platform.Mouse,
                evnt->time,
                impl.GetInputRoot(),
                evnt->type == GdkEventType.ButtonRelease
                    ? evnt->button == 1
                        ? RawMouseEventType.LeftButtonUp
                        : evnt->button == 3 ? RawMouseEventType.RightButtonUp : RawMouseEventType.MiddleButtonUp
                    : evnt->button == 1
                        ? RawMouseEventType.LeftButtonDown
                        : evnt->button == 3 ? RawMouseEventType.RightButtonDown : RawMouseEventType.MiddleButtonDown,
                new Point(evnt->x, evnt->y), GetModifierKeys(evnt->state));
            impl.OnInput(e);
            return true;
        }

        private static bool OnConfigured(IntPtr gtkWidget, IntPtr ev, IntPtr userData)
        {
            var impl = GetImplementationFromEvent(ev, true);

            if (impl != null)
            {
                impl.OnConfigured();
            }

            return false;
        }

        private static Native.D.TickCallback PinnedStaticCallback = StaticTickCallback;


        static bool StaticTickCallback(IntPtr widget, IntPtr clock, IntPtr userData)
        {
            var impl = GetImplementationFromGtkWindow(userData, true);

            if (impl != null)
                impl.OnRenderTick();
            else
                Logger.Warning("EventManager", widget, "No window to route the OnRenderTick to");

            return true;
        }

        internal static void EnterModal(WindowBaseImpl impl)
        {
            modalStack.Add(impl);
        }

        private static void RemoveSubscription(GtkWidget widget)
        {
            var impl = windowStack.FirstOrDefault(wbi => wbi.GtkWidget.Equals(widget));
            if (impl == null)
            {
                Logger.Warning("EventManager", widget, "Can't remove subscription for window");
                return;
            }

            if (modalStack.Remove(impl))
            {
                var lastModal = modalStack.LastOrDefault();
                if (lastModal == null)
                {
                    lastModal = windowStack.FirstOrDefault();
                    modalStack.Add(lastModal);
                }

                var windowEnum = windowStack.GetEnumerator();

                while (windowEnum.MoveNext() && windowEnum.Current != lastModal)
                    ; // just skip all before the top modal window 

                while (windowEnum.MoveNext())
                    Native.GtkWindowSetTransientFor(windowEnum.Current.GtkWidget, lastModal.GtkWidget);
            }

            windowStack.Remove(impl);
        }

        private sealed class EventSubscription : IDisposable
        {
            IList<IDisposable> signals = new List<IDisposable>();
            uint? _tickCallback;
            GtkWidget GtkWidget;

            public void ConnectEvent(string name, Native.D.signal_onevent handler) => signals.Add(Signal.Connect<Native.D.signal_onevent>(GtkWidget, name, handler));
            public void Connect<T>(string name, T handler, IntPtr userData) => signals.Add(Signal.Connect(GtkWidget, name, handler, userData));

            public EventSubscription(GtkWidget gtkWidget)
            {
                GtkWidget = gtkWidget;
            }

            public void AddTickCallback(IntPtr userData)
            {
                _tickCallback = Native.GtkWidgetAddTickCallback(GtkWidget, PinnedStaticCallback, userData, IntPtr.Zero);
            }

            public void Dispose()
            {
                RemoveSubscription(GtkWidget);

                if (_tickCallback.HasValue)
                {
                    if (!GtkWidget.IsClosed)
                        Native.GtkWidgetRemoveTickCallback(GtkWidget, _tickCallback.Value);
                    _tickCallback = null;
                }

                foreach (var signal in signals.Reverse())
                    signal.Dispose();

            }
        }
    }
}
