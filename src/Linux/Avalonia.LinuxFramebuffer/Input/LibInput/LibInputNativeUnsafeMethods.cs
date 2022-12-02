using System;
using System.Runtime.InteropServices;

namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    unsafe class LibInputNativeUnsafeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int OpenRestrictedCallbackDelegate(IntPtr path, int flags, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseRestrictedCallbackDelegate(int fd, IntPtr userData);

        static int OpenRestricted(IntPtr path, int flags, IntPtr userData)
        {
            var fd = NativeUnsafeMethods.open(Marshal.PtrToStringAnsi(path), flags, 0);
            if (fd == -1)
                return -Marshal.GetLastWin32Error();

            return fd;
        }

        static void CloseRestricted(int fd, IntPtr userData)
        {
            NativeUnsafeMethods.close(fd);
        }

        private static readonly IntPtr* s_Interface;

        static LibInputNativeUnsafeMethods()
        {
            s_Interface = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size * 2);

            IntPtr Convert<TDelegate>(TDelegate del)
            {
                GCHandle.Alloc(del);
                return Marshal.GetFunctionPointerForDelegate(del);
            }

            s_Interface[0] = Convert<OpenRestrictedCallbackDelegate>(OpenRestricted);
            s_Interface[1] = Convert<CloseRestrictedCallbackDelegate>(CloseRestricted);
        }

        private const string LibInput = "libinput.so.10";
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_create_context(IntPtr* iface, IntPtr userData);

        public static IntPtr libinput_path_create_context() =>
            libinput_path_create_context(s_Interface, IntPtr.Zero);

        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_add_device(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_remove_device(IntPtr device);
        
        [DllImport(LibInput)]
        public extern static int libinput_get_fd(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static void libinput_dispatch(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_get_event(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static LibInputEventType libinput_event_get_type(IntPtr ev);

        public enum LibInputEventType
        {
            LIBINPUT_EVENT_NONE = 0,
            LIBINPUT_EVENT_DEVICE_ADDED,
            LIBINPUT_EVENT_DEVICE_REMOVED,
            LIBINPUT_EVENT_KEYBOARD_KEY = 300,
            LIBINPUT_EVENT_POINTER_MOTION = 400,
            LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE,
            LIBINPUT_EVENT_POINTER_BUTTON,
            LIBINPUT_EVENT_POINTER_AXIS,
            LIBINPUT_EVENT_POINTER_SCROLL_WHEEL,
            LIBINPUT_EVENT_POINTER_SCROLL_FINGER,
            LIBINPUT_EVENT_POINTER_SCROLL_CONTINUOUS,
            LIBINPUT_EVENT_TOUCH_DOWN = 500,
            LIBINPUT_EVENT_TOUCH_UP,
            LIBINPUT_EVENT_TOUCH_MOTION,
            LIBINPUT_EVENT_TOUCH_CANCEL,
            LIBINPUT_EVENT_TOUCH_FRAME,
            LIBINPUT_EVENT_TABLET_TOOL_AXIS = 600,
            LIBINPUT_EVENT_TABLET_TOOL_PROXIMITY,
            LIBINPUT_EVENT_TABLET_TOOL_TIP,
            LIBINPUT_EVENT_TABLET_TOOL_BUTTON,
            LIBINPUT_EVENT_TABLET_PAD_BUTTON = 700,
            LIBINPUT_EVENT_TABLET_PAD_RING,
            LIBINPUT_EVENT_TABLET_PAD_STRIP,
            LIBINPUT_EVENT_GESTURE_SWIPE_BEGIN = 800,
            LIBINPUT_EVENT_GESTURE_SWIPE_UPDATE,
            LIBINPUT_EVENT_GESTURE_SWIPE_END,
            LIBINPUT_EVENT_GESTURE_PINCH_BEGIN,
            LIBINPUT_EVENT_GESTURE_PINCH_UPDATE,
            LIBINPUT_EVENT_GESTURE_PINCH_END,
            LIBINPUT_EVENT_SWITCH_TOGGLE = 900,
        }

        public enum LibInputPointerAxisSource
        {
            /**
             * The event is caused by the rotation of a wheel.
            **/
            LIBINPUT_POINTER_AXIS_SOURCE_WHEEL = 1,
            /**
             * The event is caused by the movement of one or more fingers on a device.
             **/
            LIBINPUT_POINTER_AXIS_SOURCE_FINGER,
            /**
             * The event is caused by the motion of some device.
             **/
            LIBINPUT_POINTER_AXIS_SOURCE_CONTINUOUS,
            /**
             * The event is caused by the tilting of a mouse wheel rather than
             * its rotation. This method is commonly used on mice without
             * separate horizontal scroll wheels.
             * @deprecated This axis source is deprecated as of libinput 1.16.
             * It was never used by any device before libinput 1.16. All wheel
             * tilt devices use @ref LIBINPUT_POINTER_AXIS_SOURCE_WHEEL instead.
             **/
            LIBINPUT_POINTER_AXIS_SOURCE_WHEEL_TILT,
        };

        public enum LibInputPointerAxis
        {
            LIBINPUT_POINTER_AXIS_SCROLL_VERTICAL = 0,
            LIBINPUT_POINTER_AXIS_SCROLL_HORIZONTAL = 1,
        };

        [DllImport(LibInput)]
        public extern static void libinput_event_destroy(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_event_get_touch_event(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static int libinput_event_touch_get_slot(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static ulong libinput_event_touch_get_time_usec(IntPtr ev);

        [DllImport(LibInput)]
        public extern static double libinput_event_touch_get_x_transformed(IntPtr ev, int width);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_touch_get_y_transformed(IntPtr ev, int height);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_event_get_pointer_event(IntPtr ev);

        [DllImport(LibInput)]
        public extern static ulong libinput_event_pointer_get_time_usec(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_absolute_x_transformed(IntPtr ev, int width);

        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_absolute_y_transformed(IntPtr ev, int height);

        [DllImport(LibInput)]
        public extern static int libinput_event_pointer_get_button(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static int libinput_event_pointer_get_button_state(IntPtr ev);

        [DllImport(LibInput)]
        public extern static LibInputPointerAxisSource libinput_event_pointer_get_axis_source(IntPtr ev);

        [DllImport((LibInput))]
        public extern static double libinput_event_pointer_get_axis_value_discrete(IntPtr ev, LibInputPointerAxis axis);

        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_scroll_value_v120(IntPtr ev, LibInputPointerAxis axis);
    }
}
