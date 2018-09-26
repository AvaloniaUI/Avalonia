using System;
using Avalonia.Threading;

namespace Avalonia.Gtk3.Interop
{
    static class GlibPriority
    {
        public static int High = -100;
        public static int Default = 0;
        public static int HighIdle = 100;
        public static int GtkResize = HighIdle + 10;
        public static int GtkPaint = HighIdle + 20;
        public static int DefaultIdle = 200;
        public static int Low = 300;
        public static int GdkEvents = Default;
        public static int GdkRedraw = HighIdle + 20;

        public static int FromDispatcherPriority(DispatcherPriority prio)
        {
            if (prio == DispatcherPriority.Send)
                return High;
            if (prio == DispatcherPriority.Normal)
                return Default;
            if (prio == DispatcherPriority.DataBind)
                return Default + 1;
            if (prio == DispatcherPriority.Layout)
                return Default + 2;
            if (prio == DispatcherPriority.Render)
                return Default + 3;
            if (prio == DispatcherPriority.Loaded)
                return GtkPaint + 20;
            if (prio == DispatcherPriority.Input)
                return GtkPaint + 21;
            if (prio == DispatcherPriority.Background)
                return DefaultIdle + 1;
            if (prio == DispatcherPriority.ContextIdle)
                return DefaultIdle + 2;
            if (prio == DispatcherPriority.ApplicationIdle)
                return DefaultIdle + 3;
            if (prio == DispatcherPriority.SystemIdle)
                return DefaultIdle + 4;
            throw new ArgumentException("Unknown priority");

        }
    }
}
