using System;
using System.Runtime.InteropServices;
using System.Text;

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
        const string libX11 = "X11";

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
        public static extern int XRaiseWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XLowerWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XConfigureWindow(IntPtr display, IntPtr window, ChangeWindowFlags value_mask,
            ref XWindowChanges values);

        [DllImport(libX11)]
        public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        [DllImport(libX11)]
        public static extern int XInternAtoms(IntPtr display, string[] atom_names, int atom_count, bool only_if_exists,
            IntPtr[] atoms);

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
        public static extern int XSetTransientForHint(IntPtr display, IntPtr window, IntPtr prop_window);

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
            int format, PropertyMode mode, uint[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, int[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, IntPtr[] data, int nelements);

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



    }
}
