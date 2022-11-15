using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Platform.Interop;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable CommentTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Avalonia.X11
{
    internal unsafe static class XLib
    {
        const string libX11 = "libX11.so.6";
        const string libX11Randr = "libXrandr.so.2";
        const string libX11Ext = "libXext.so.6";
        const string libXInput = "libXi.so.6";
        const string libXCursor = "libXcursor.so.1";

        [DllImport(libX11)]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XSynchronize(IntPtr display, bool onoff);

        [DllImport(libX11)]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height,
            int border_width, int depth, int xclass, IntPtr visual, UIntPtr valuemask,
            ref XSetWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
            int height, int border_width, IntPtr border, IntPtr background);

        [DllImport(libX11)]
        public static extern int XMapWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XUnmapWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XMapSubindows(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XUnmapSubwindows(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern IntPtr XRootWindow(IntPtr display, int screen_number);
        [DllImport(libX11)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XNextEvent(IntPtr display, out XEvent xevent);
        
        [DllImport(libX11)]
        public static extern IntPtr XNextEvent(IntPtr display, XEvent* xevent);

        [DllImport(libX11)]
        public static extern int XConnectionNumber(IntPtr diplay);

        [DllImport(libX11)]
        public static extern int XPending(IntPtr diplay);

        [DllImport(libX11)]
        public static extern IntPtr XSelectInput(IntPtr display, IntPtr window, IntPtr mask);

        [DllImport(libX11)]
        public static extern int XDestroyWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);

        [DllImport(libX11)]
        public static extern int XMoveResizeWindow(IntPtr display, IntPtr window, int x, int y, int width, int height);

        [DllImport(libX11)]
        public static extern int XResizeWindow(IntPtr display, IntPtr window, int width, int height);

        [DllImport(libX11)]
        public static extern int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern int XFlush(IntPtr display);

        [DllImport(libX11)]
        public static extern int XSetWMName(IntPtr display, IntPtr window, ref XTextProperty text_prop);

        [DllImport(libX11)]
        public static extern int XStoreName(IntPtr display, IntPtr window, string window_name);

        [DllImport(libX11)]
        public static extern int XFetchName(IntPtr display, IntPtr window, ref IntPtr window_name);

        [DllImport(libX11)]
        public static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr event_mask,
            ref XEvent send_event);

        [DllImport(libX11)]
        public static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return,
            out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

        [DllImport(libX11)]
        public static extern int XFree(IntPtr data);
        
        [DllImport(libX11)]
        public static extern int XFree(void* data);

        [DllImport(libX11)]
        public static extern int XRaiseWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XLowerWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XConfigureWindow(IntPtr display, IntPtr window, ChangeWindowFlags value_mask,
            ref XWindowChanges values);

        public static uint XConfigureResizeWindow(IntPtr display, IntPtr window, PixelSize size)
            => XConfigureResizeWindow(display, window, size.Width, size.Height);
        
        public static uint XConfigureResizeWindow(IntPtr display, IntPtr window, int width, int height)
        {
            var changes = new XWindowChanges
            {
                width = width,
                height = height
            };

            return XConfigureWindow(display, window, ChangeWindowFlags.CWHeight | ChangeWindowFlags.CWWidth,
                ref changes);
        }

        [DllImport(libX11)]
        public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        [DllImport(libX11)]
        public static extern int XInternAtoms(IntPtr display, string[] atom_names, int atom_count, bool only_if_exists,
            IntPtr[] atoms);
        
        [DllImport(libX11)]
        public static extern IntPtr XGetAtomName(IntPtr display, IntPtr atom);

        public static string GetAtomName(IntPtr display, IntPtr atom)
        {
            var ptr = XGetAtomName(display, atom);
            if (ptr == IntPtr.Zero)
                return null;
            var s = Marshal.PtrToStringAnsi(ptr);
            XFree(ptr);
            return s;
        }

        [DllImport(libX11)]
        public static extern int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);

        [DllImport(libX11)]
        public static extern int XGrabPointer(IntPtr display, IntPtr window, bool owner_events, EventMask event_mask,
            GrabMode pointer_mode, GrabMode keyboard_mode, IntPtr confine_to, IntPtr cursor, IntPtr timestamp);

        [DllImport(libX11)]
        public static extern int XUngrabPointer(IntPtr display, IntPtr timestamp);

        [DllImport(libX11)]
        public static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
            out int root_x, out int root_y, out int win_x, out int win_y, out int keys_buttons);

        [DllImport(libX11)]
        public static extern bool XTranslateCoordinates(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x,
            int src_y, out int intdest_x_return, out int dest_y_return, out IntPtr child_return);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y,
            out int width, out int height, out int border_width, out int depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, out int x, out int y,
            out int width, out int height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, out int x, out int y,
            IntPtr width, IntPtr height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, IntPtr x, IntPtr y,
            out int width, out int height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern uint XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y,
            uint src_width, uint src_height, int dest_x, int dest_y);

        [DllImport(libX11)]
        public static extern int XClearWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XClearArea(IntPtr display, IntPtr window, int x, int y, int width, int height,
            bool exposures);

        // Colormaps
        [DllImport(libX11)]
        public static extern IntPtr XDefaultScreenOfDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XScreenNumberOfScreen(IntPtr display, IntPtr Screen);

        [DllImport(libX11)]
        public static extern IntPtr XDefaultVisual(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern uint XDefaultDepth(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern int XDefaultScreen(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XDefaultColormap(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern int XLookupColor(IntPtr display, IntPtr Colormap, string Coloranem,
            ref XColor exact_def_color, ref XColor screen_def_color);

        [DllImport(libX11)]
        public static extern int XAllocColor(IntPtr display, IntPtr Colormap, ref XColor colorcell_def);

        [DllImport(libX11)]
        public static extern int XSetTransientForHint(IntPtr display, IntPtr window, IntPtr parent);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref MotifWmHints data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref uint value, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref IntPtr value, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, byte[] data, int nelements);
        
        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, uint[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, int[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, IntPtr[] data, int nelements);
        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, void* data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, IntPtr atoms, int nelements);

        [DllImport(libX11, CharSet = CharSet.Ansi)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, string text, int text_length);

        [DllImport(libX11)]
        public static extern int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);

        // Drawing
        [DllImport(libX11)]
        public static extern IntPtr XCreateGC(IntPtr display, IntPtr window, IntPtr valuemask, ref XGCValues values);

        [DllImport(libX11)]
        public static extern int XFreeGC(IntPtr display, IntPtr gc);

        [DllImport(libX11)]
        public static extern int XSetFunction(IntPtr display, IntPtr gc, GXFunction function);

        [DllImport(libX11)]
        internal static extern int XSetLineAttributes(IntPtr display, IntPtr gc, int line_width, GCLineStyle line_style,
            GCCapStyle cap_style, GCJoinStyle join_style);

        [DllImport(libX11)]
        public static extern int XDrawLine(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int x2, int y2);

        [DllImport(libX11)]
        public static extern int XDrawRectangle(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int width,
            int height);

        [DllImport(libX11)]
        public static extern int XFillRectangle(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int width,
            int height);

        [DllImport(libX11)]
        public static extern int XSetWindowBackground(IntPtr display, IntPtr window, IntPtr background);

        [DllImport(libX11)]
        public static extern int XCopyArea(IntPtr display, IntPtr src, IntPtr dest, IntPtr gc, int src_x, int src_y,
            int width, int height, int dest_x, int dest_y);

        [DllImport(libX11)]
        public static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset,
            IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format,
            out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

        [DllImport(libX11)]
        public static extern int XSetInputFocus(IntPtr display, IntPtr window, RevertTo revert_to, IntPtr time);

        [DllImport(libX11)]
        public static extern int XIconifyWindow(IntPtr display, IntPtr window, int screen_number);

        [DllImport(libX11)]
        public static extern int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

        [DllImport(libX11)]
        public static extern int XUndefineCursor(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XFreeCursor(IntPtr display, IntPtr cursor);

        [DllImport(libX11)]
        public static extern IntPtr XCreateFontCursor(IntPtr display, CursorFontShape shape);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmapCursor(IntPtr display, IntPtr source, IntPtr mask,
            ref XColor foreground_color, ref XColor background_color, int x_hot, int y_hot);

        [DllImport(libX11)]
        public static extern IntPtr XCreateBitmapFromData(IntPtr display, IntPtr drawable, byte[] data, int width, int height);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmapFromBitmapData(IntPtr display, IntPtr drawable, byte[] data, int width,
            int height, IntPtr fg, IntPtr bg, int depth);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmap(IntPtr display, IntPtr d, int width, int height, int depth);

        [DllImport(libX11)]
        public static extern IntPtr XFreePixmap(IntPtr display, IntPtr pixmap);

        [DllImport(libX11)]
        public static extern int XQueryBestCursor(IntPtr display, IntPtr drawable, int width, int height,
            out int best_width, out int best_height);

        [DllImport(libX11)]
        public static extern IntPtr XWhitePixel(IntPtr display, int screen_no);

        [DllImport(libX11)]
        public static extern IntPtr XBlackPixel(IntPtr display, int screen_no);

        [DllImport(libX11)]
        public static extern void XGrabServer(IntPtr display);

        [DllImport(libX11)]
        public static extern void XUngrabServer(IntPtr display);

        [DllImport(libX11)]
        public static extern void XGetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints,
            out IntPtr supplied_return);

        [DllImport(libX11)]
        public static extern void XSetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints);

        [DllImport(libX11)]
        public static extern void XSetZoomHints(IntPtr display, IntPtr window, ref XSizeHints hints);

        [DllImport(libX11)]
        public static extern void XSetWMHints(IntPtr display, IntPtr window, ref XWMHints wmhints);

        [DllImport(libX11)]
        public static extern int XGetIconSizes(IntPtr display, IntPtr window, out IntPtr size_list, out int count);

        [DllImport(libX11)]
        public static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);

        [DllImport(libX11)]
        public static extern IntPtr XGetErrorText(IntPtr display, byte code, StringBuilder buffer, int length);

        [DllImport(libX11)]
        public static extern int XInitThreads();

        [DllImport(libX11)]
        public static extern int XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property,
            IntPtr requestor, IntPtr time);

        [DllImport(libX11)]
        public static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

        [DllImport(libX11)]
        public static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

        [DllImport(libX11)]
        public static extern int XSetPlaneMask(IntPtr display, IntPtr gc, IntPtr mask);

        [DllImport(libX11)]
        public static extern int XSetForeground(IntPtr display, IntPtr gc, UIntPtr foreground);

        [DllImport(libX11)]
        public static extern int XSetBackground(IntPtr display, IntPtr gc, UIntPtr background);

        [DllImport(libX11)]
        public static extern int XBell(IntPtr display, int percent);

        [DllImport(libX11)]
        public static extern int XChangeActivePointerGrab(IntPtr display, EventMask event_mask, IntPtr cursor,
            IntPtr time);

        [DllImport(libX11)]
        public static extern bool XFilterEvent(ref XEvent xevent, IntPtr window);
        
        [DllImport(libX11)]
        public static extern bool XFilterEvent(XEvent* xevent, IntPtr window);

        [DllImport(libX11)]
        public static extern void XkbSetDetectableAutoRepeat(IntPtr display, bool detectable, IntPtr supported);

        [DllImport(libX11)]
        public static extern void XPeekEvent(IntPtr display, out XEvent xevent);
        
        [DllImport(libX11)]
        public static extern void XMatchVisualInfo(IntPtr display, int screen, int depth, int klass, out XVisualInfo info);
        
        [DllImport(libX11)]
        public static extern IntPtr XLockDisplay(IntPtr display);
        
        [DllImport(libX11)]
        public static extern IntPtr XUnlockDisplay(IntPtr display);
        
        [DllImport(libX11)]
        public static extern IntPtr XCreateGC(IntPtr display, IntPtr drawable, ulong valuemask, IntPtr values);
        
        [DllImport(libX11)]
        public static extern int XInitImage(ref XImage image);
        
        [DllImport(libX11)]
        public static extern int XDestroyImage(ref XImage image);

        [DllImport(libX11)]
        public static extern int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, ref XImage image,
            int srcx, int srcy, int destx, int desty, uint width, uint height);
        [DllImport(libX11)]
        public static extern int XSync(IntPtr display, bool discard);
        
        [DllImport(libX11)]
        public static extern IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int create);
        
        public enum XLookupStatus : uint
        {
            XBufferOverflow = 0xffffffffu,
            XLookupNone = 1,
            XLookupChars = 2,
            XLookupKeySym = 3,
            XLookupBoth = 4
        }
        
        [DllImport (libX11)]
        public static extern unsafe int XLookupString(ref XEvent xevent, void* buffer, int num_bytes, out IntPtr keysym, out IntPtr status);
        
        [DllImport (libX11)]
        public static extern unsafe int Xutf8LookupString(IntPtr xic, ref XEvent xevent, void* buffer, int num_bytes, out IntPtr keysym, out UIntPtr status);
        
        [DllImport (libX11)]
        public static extern unsafe int Xutf8LookupString(IntPtr xic, XEvent* xevent, void* buffer, int num_bytes, out IntPtr keysym, out IntPtr status);
        
        [DllImport (libX11)]
        public static extern unsafe IntPtr XKeycodeToKeysym(IntPtr display, int keycode, int index);
        
        [DllImport (libX11)]
        public static extern unsafe IntPtr XSetLocaleModifiers(string modifiers);

        [DllImport (libX11)]
        public static extern IntPtr XOpenIM (IntPtr display, IntPtr rdb, IntPtr res_name, IntPtr res_class);
        
        [DllImport (libX11)]
        public static extern IntPtr XGetIMValues (IntPtr xim, string name, out XIMStyles* value, IntPtr terminator);
        
        [DllImport (libX11)]
        public static extern IntPtr XCreateIC (IntPtr xim, string name, IntPtr value, string name2, IntPtr value2, string name3, IntPtr value3, IntPtr terminator);

        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string name, IntPtr value, string name2, IntPtr value2,
            string name3, IntPtr value3, string name4, IntPtr value4, IntPtr terminator);

        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string xnClientWindow, IntPtr handle, 
            string xnInputStyle, IntPtr value3, string xnResourceName, string optionsWmClass,
            string xnResourceClass, string wmClass, string xnPreeditAttributes, IntPtr list, IntPtr zero);
        
        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string xnClientWindow, IntPtr handle, string xnFocusWindow,
            IntPtr value2, string xnInputStyle, IntPtr value3, string xnResourceName, string optionsWmClass,
            string xnResourceClass, string wmClass, string xnPreeditAttributes, IntPtr list, IntPtr zero);

        [DllImport(libX11)]
        public static extern void XSetICFocus(IntPtr xic);
        
        [DllImport(libX11)]
        public static extern void XUnsetICFocus(IntPtr xic);
        
        [DllImport(libX11)]
        public static extern IntPtr XmbResetIC(IntPtr xic);

        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int unused, Utf8Buffer name, ref XPoint point, IntPtr terminator);
        
        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int unused, Utf8Buffer xnArea, XRectangle* point,
            Utf8Buffer xnSpotLocation, XPoint* value2, Utf8Buffer xnFontSet, IntPtr fs, IntPtr zero);
        
        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int unused,
            Utf8Buffer xnSpotLocation, XPoint* value2, Utf8Buffer xnFontSet, IntPtr fs, IntPtr zero);
        
        [DllImport (libX11)]
        public static extern IntPtr XCreateFontSet (IntPtr display, string name, out IntPtr list, out int count, IntPtr unused);
        
        [DllImport(libX11)]
        public static extern IntPtr XSetICValues(IntPtr ic, string name, IntPtr data, IntPtr terminator);
        
        [DllImport (libX11)]
        public static extern void XCloseIM (IntPtr xim);

        [DllImport (libX11)]
        public static extern void XDestroyIC (IntPtr xic);

        [DllImport(libX11)]
        public static extern bool XQueryExtension(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string name,
            out int majorOpcode, out int firstEvent, out int firstError);

        [DllImport(libX11)]
        public static extern bool XGetEventData(IntPtr display, void* cookie);

        [DllImport(libX11)]
        public static extern void XFreeEventData(IntPtr display, void* cookie);
        
        [DllImport(libX11Randr)]
        public static extern int XRRQueryExtension (IntPtr dpy,
            out int event_base_return,
            out int error_base_return);
        
        [DllImport(libX11Ext)]
        public static extern Status XSyncInitialize(IntPtr dpy, out int event_base_return, out int error_base_return);

        [DllImport(libX11Ext)]
        public static extern IntPtr XSyncCreateCounter(IntPtr dpy, XSyncValue initialValue);
        
        [DllImport(libX11Ext)]
        public static extern int XSyncDestroyCounter(IntPtr dpy, IntPtr counter);
        
        [DllImport(libX11Ext)]
        public static extern int XSyncSetCounter(IntPtr dpy, IntPtr counter, XSyncValue value);

        [DllImport(libX11Randr)]
        public static extern int XRRQueryVersion(IntPtr dpy,
            out int major_version_return,
            out int minor_version_return);

        [DllImport(libX11Randr)]
        public static extern XRRMonitorInfo*
            XRRGetMonitors(IntPtr dpy, IntPtr window, bool get_active, out int nmonitors);

        [DllImport(libX11Randr)]
        public static extern IntPtr* XRRListOutputProperties(IntPtr dpy, IntPtr output, out int count);

        [DllImport(libX11Randr)]
        public static extern int XRRGetOutputProperty(IntPtr dpy, IntPtr output, IntPtr atom, int offset, int length, bool _delete, bool pending, IntPtr req_type, out IntPtr actual_type, out int actual_format, out int nitems, out long bytes_after, out IntPtr prop);
            
        [DllImport(libX11Randr)]
        public static extern void XRRSelectInput(IntPtr dpy, IntPtr window, RandrEventMask mask);

        [DllImport(libXInput)]
        public static extern Status XIQueryVersion(IntPtr dpy, ref int major, ref int minor);

        [DllImport(libXInput)]
        public static extern IntPtr XIQueryDevice(IntPtr dpy, int deviceid, out int ndevices_return);

        [DllImport(libXInput)]
        public static extern void XIFreeDeviceInfo(XIDeviceInfo* info);

        [DllImport(libXCursor)]
        public static extern IntPtr XcursorImageLoadCursor(IntPtr display, IntPtr image);

        [DllImport(libXCursor)]
        public static extern IntPtr XcursorImageDestroy(IntPtr image);

        public static void XISetMask(ref int mask, XiEventType ev)
        {
            mask |= (1 << (int)ev);
        }
        
        public static int XiEventMaskLen { get; } = 4;

        public static bool XIMaskIsSet(void* ptr, int shift) =>
            (((byte*)(ptr))[(shift) >> 3] & (1 << (shift & 7))) != 0;

        [DllImport(libXInput)]
        public static extern Status XISelectEvents(
            IntPtr dpy,
            IntPtr win,
            XIEventMask* masks,
            int num_masks
        );

        public static Status XiSelectEvents(IntPtr display, IntPtr window, Dictionary<int, List<XiEventType>> devices)
        {
            var masks = stackalloc int[devices.Count];
            var emasks = stackalloc XIEventMask[devices.Count];
            int c = 0;
            foreach (var d in devices)
            {
                foreach (var ev in d.Value)
                    XISetMask(ref masks[c], ev);
                emasks[c] = new XIEventMask
                {
                    Mask = &masks[c],
                    Deviceid = d.Key,
                    MaskLen = XiEventMaskLen
                };
                c++;
            }


            return XISelectEvents(display, window, emasks, devices.Count);

        }
        
        [DllImport(libX11)]
        public static extern XClassHint* XAllocClassHint();

        [DllImport(libX11)]
        public static extern int XSetClassHint(IntPtr display, IntPtr window, XClassHint* class_hints);

        public struct XGeometry
        {
            public IntPtr root;
            public int x;
            public int y;
            public int width;
            public int height;
            public int bw;
            public int d;
        }
        public struct XClassHint
        {
            public byte* res_name;
            public byte* res_class;
        }
        
        public struct XSyncValue {
            public int Hi;
            public uint Lo;
        }

        public static bool XGetGeometry(IntPtr display, IntPtr window, out XGeometry geo)
        {
            geo = new XGeometry();
            return XGetGeometry(display, window, out geo.root, out geo.x, out geo.y, out geo.width, out geo.height,
                out geo.bw, out geo.d);
        }
        
        public static void QueryPointer (IntPtr display, IntPtr w, out IntPtr root, out IntPtr child,
            out int root_x, out int root_y, out int child_x, out int child_y,
            out int mask)
        {

            IntPtr c;

            XGrabServer (display);

            XQueryPointer(display, w, out root, out c,
                out root_x, out root_y, out child_x, out child_y,
                out mask);

            if (root != w)
                c = root;

            IntPtr child_last = IntPtr.Zero;
            while (c != IntPtr.Zero) {
                child_last = c;
                XQueryPointer(display, c, out root, out c,
                    out root_x, out root_y, out child_x, out child_y,
                    out mask);
            }
            XUngrabServer (display);
            XFlush (display);

            child = child_last;
        }

        public static (int x, int y) GetCursorPos(X11Info x11, IntPtr? handle = null)
        {
            IntPtr root;
            IntPtr child;
            int root_x;
            int root_y;
            int win_x;
            int win_y;
            int keys_buttons;



            QueryPointer(x11.Display, handle ?? x11.RootWindow, out root, out child, out root_x, out root_y, out win_x, out win_y,
                out keys_buttons);


            if (handle != null)
            {
                return (win_x, win_y);
            }
            else
            {
                return (root_x, root_y);
            }
        }

        public static IntPtr CreateEventWindow(AvaloniaX11Platform plat, X11PlatformThreading.EventHandler handler)
        {
            var win = XCreateSimpleWindow(plat.Display, plat.Info.DefaultRootWindow, 
                0, 0, 1, 1, 0, IntPtr.Zero, IntPtr.Zero);
            plat.Windows[win] = handler;
            return win;
        }
    }
}
