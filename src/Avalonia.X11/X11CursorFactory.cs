using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class X11CursorFactory : IStandardCursorFactory
    {
        private static IntPtr _nullCursor;

        private readonly IntPtr _display;
        private Dictionary<CursorFontShape, IntPtr> _cursors;

        private static readonly Dictionary<StandardCursorType, CursorFontShape> s_mapping =
            new Dictionary<StandardCursorType, CursorFontShape>
            {
                {StandardCursorType.Arrow, CursorFontShape.XC_top_left_arrow},
                {StandardCursorType.Cross, CursorFontShape.XC_cross},
                {StandardCursorType.Hand, CursorFontShape.XC_hand1},
                {StandardCursorType.Help, CursorFontShape.XC_question_arrow},
                {StandardCursorType.Ibeam, CursorFontShape.XC_xterm},
                {StandardCursorType.No, CursorFontShape.XC_X_cursor},
                {StandardCursorType.Wait, CursorFontShape.XC_watch},
                {StandardCursorType.AppStarting, CursorFontShape.XC_watch},
                {StandardCursorType.BottomSize, CursorFontShape.XC_bottom_side},
                {StandardCursorType.DragCopy, CursorFontShape.XC_center_ptr},
                {StandardCursorType.DragLink, CursorFontShape.XC_fleur},
                {StandardCursorType.DragMove, CursorFontShape.XC_diamond_cross},
                {StandardCursorType.LeftSide, CursorFontShape.XC_left_side},
                {StandardCursorType.RightSide, CursorFontShape.XC_right_side},
                {StandardCursorType.SizeAll, CursorFontShape.XC_sizing},
                {StandardCursorType.TopSide, CursorFontShape.XC_top_side},
                {StandardCursorType.UpArrow, CursorFontShape.XC_sb_up_arrow},
                {StandardCursorType.BottomLeftCorner, CursorFontShape.XC_bottom_left_corner},
                {StandardCursorType.BottomRightCorner, CursorFontShape.XC_bottom_right_corner},
                {StandardCursorType.SizeNorthSouth, CursorFontShape.XC_sb_v_double_arrow},
                {StandardCursorType.SizeWestEast, CursorFontShape.XC_sb_h_double_arrow},
                {StandardCursorType.TopLeftCorner, CursorFontShape.XC_top_left_corner},
                {StandardCursorType.TopRightCorner, CursorFontShape.XC_top_right_corner},
            };

        public X11CursorFactory(IntPtr display)
        {
            _display = display;
            _nullCursor = GetNullCursor(display);
            _cursors = Enum.GetValues(typeof(CursorFontShape)).Cast<CursorFontShape>()
                .ToDictionary(id => id, id => XLib.XCreateFontCursor(_display, id));
        }

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            IntPtr handle;
            if (cursorType == StandardCursorType.None)
            {
                handle = _nullCursor;
            }
            else
            {
                handle = s_mapping.TryGetValue(cursorType, out var shape)
                ? _cursors[shape]
                : _cursors[CursorFontShape.XC_top_left_arrow];
            }
            return new PlatformHandle(handle, "XCURSOR");
        }

        private static IntPtr GetNullCursor(IntPtr display)
        {
            XColor color = new XColor();
            byte[] data = new byte[] { 0 };
            IntPtr window = XLib.XRootWindow(display, 0);
            IntPtr pixmap = XLib.XCreateBitmapFromData(display, window, data, 1, 1);
            return XLib.XCreatePixmapCursor(display, pixmap, pixmap, ref color, ref color, 0, 0);
        }
    }
}
