using System;
using System.Collections.Generic;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Platform;
using CursorType = Avalonia.Gtk3.GdkCursorType;
namespace Avalonia.Gtk3
{
    class CursorFactory :  IStandardCursorFactory
    {
        private static readonly Dictionary<StandardCursorType, object> CursorTypeMapping = new Dictionary
    <StandardCursorType, object>
        {
            {StandardCursorType.AppStarting, CursorType.Watch},
            {StandardCursorType.Arrow, CursorType.LeftPtr},
            {StandardCursorType.Cross, CursorType.Cross},
            {StandardCursorType.Hand, CursorType.Hand1},
            {StandardCursorType.Ibeam, CursorType.Xterm},
            {StandardCursorType.No, "gtk-cancel"},
            {StandardCursorType.SizeAll, CursorType.Sizing},
            //{ StandardCursorType.SizeNorthEastSouthWest, 32643 },
            {StandardCursorType.SizeNorthSouth, CursorType.SbVDoubleArrow},
            //{ StandardCursorType.SizeNorthWestSouthEast, 32642 },
            {StandardCursorType.SizeWestEast, CursorType.SbHDoubleArrow},
            {StandardCursorType.UpArrow, CursorType.BasedArrowUp},
            {StandardCursorType.Wait, CursorType.Watch},
            {StandardCursorType.Help, "gtk-help"},
            {StandardCursorType.TopSide, CursorType.TopSide},
            {StandardCursorType.BottomSize, CursorType.BottomSide},
            {StandardCursorType.LeftSide, CursorType.LeftSide},
            {StandardCursorType.RightSide, CursorType.RightSide},
            {StandardCursorType.TopLeftCorner, CursorType.TopLeftCorner},
            {StandardCursorType.TopRightCorner, CursorType.TopRightCorner},
            {StandardCursorType.BottomLeftCorner, CursorType.BottomLeftCorner},
            {StandardCursorType.BottomRightCorner, CursorType.BottomRightCorner},
            {StandardCursorType.DragCopy, CursorType.CenterPtr},
            {StandardCursorType.DragMove, CursorType.Fleur},
            {StandardCursorType.DragLink, CursorType.Cross},
        };

        private static readonly Dictionary<StandardCursorType, IPlatformHandle> Cache =
            new Dictionary<StandardCursorType, IPlatformHandle>();

        private IntPtr GetCursor(object desc)
        {
            IntPtr rv;
            var name = desc as string;
            if (name != null)
            {
                var theme = Native.GtkIconThemeGetDefault();
                IntPtr icon, error;
                using (var u = new Utf8Buffer(name))
                    icon = Native.GtkIconThemeLoadIcon(theme, u, 32, 0, out error);
                rv = icon == IntPtr.Zero
                    ? Native.GdkCursorNew(GdkCursorType.XCursor)
                    : Native.GdkCursorNewFromPixbuf(Native.GdkGetDefaultDisplay(), icon, 0, 0);
            }
            else
            {
                rv = Native.GdkCursorNew((CursorType)desc);
            }

            
            return rv;
        }

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            IPlatformHandle rv;
            if (!Cache.TryGetValue(cursorType, out rv))
            {
                Cache[cursorType] =
                    rv =
                        new PlatformHandle(
                            GetCursor(CursorTypeMapping[cursorType]),
                            "GTKCURSOR");
            }

            return rv;
        }
    }
}