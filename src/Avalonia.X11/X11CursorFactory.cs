using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Internal;

namespace Avalonia.X11
{
    internal partial class X11CursorFactory : ICursorFactory
    {
        private static readonly byte[] NullCursorData = new byte[] { 0 };

        private static IntPtr _nullCursor;

        private readonly IntPtr _display;
        private Dictionary<StandardCursorType, IntPtr> _cursors;

        private static readonly Dictionary<StandardCursorType, CursorFontShape> s_mapping =
            new Dictionary<StandardCursorType, CursorFontShape>
            {
                {StandardCursorType.Arrow, CursorFontShape.XC_left_ptr},
                {StandardCursorType.Cross, CursorFontShape.XC_cross},
                {StandardCursorType.Hand, CursorFontShape.XC_hand2},
                {StandardCursorType.Help, CursorFontShape.XC_question_arrow},
                {StandardCursorType.Ibeam, CursorFontShape.XC_xterm},
                {StandardCursorType.No, CursorFontShape.XC_X_cursor},
                {StandardCursorType.Wait, CursorFontShape.XC_watch},
                {StandardCursorType.AppStarting, CursorFontShape.XC_watch},
                {StandardCursorType.BottomSide, CursorFontShape.XC_bottom_side},
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

        private static readonly Dictionary<StandardCursorType, string> s_libraryCursors = new()
        {
            {StandardCursorType.DragCopy, "dnd-copy"},
            {StandardCursorType.DragLink, "dnd-link"},
            {StandardCursorType.DragMove, "dnd-move"},
            // TODO: Check if other platforms have dnd-none, dnd-no-drop and dnd-ask
        };

        public X11CursorFactory(IntPtr display)
        {
            _display = display;
            _nullCursor = GetNullCursor(display);
            
            _cursors = new Dictionary<StandardCursorType, IntPtr>();
        }

        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            IntPtr handle;
            if (cursorType == StandardCursorType.None)
                handle = _nullCursor;
            else
                handle = GetCursorHandleCached(cursorType);
            return new CursorImpl(handle);
        }

        public unsafe ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            return new XImageCursor(_display, cursor, hotSpot);
        }

        private static IntPtr GetNullCursor(IntPtr display)
        {
            XColor color = new XColor();
            IntPtr window = XLib.XRootWindow(display, 0);
            IntPtr pixmap = XLib.XCreateBitmapFromData(display, window, NullCursorData, 1, 1);
            return XLib.XCreatePixmapCursor(display, pixmap, pixmap, ref color, ref color, 0, 0);
        }

        private unsafe class XImageCursor : CursorImpl, IFramebufferPlatformSurface, IPlatformHandle
        {
            private readonly IntPtr _display;
            private readonly PixelSize _pixelSize;
            private readonly UnmanagedBlob _blob;

            public XImageCursor(IntPtr display, IBitmapImpl bitmap, PixelPoint hotSpot)
            {
                var size = Marshal.SizeOf<XcursorImage>() +
                    (bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4);
                var platformRenderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

                _display = display;
                _pixelSize = bitmap.PixelSize;
                _blob = new UnmanagedBlob(size);
                
                var image = (XcursorImage*)_blob.Address;
                image->version = 1;
                image->size = Marshal.SizeOf<XcursorImage>();
                image->width = bitmap.PixelSize.Width;
                image->height = bitmap.PixelSize.Height;
                image->xhot = hotSpot.X;
                image->yhot = hotSpot.Y;
                image->pixels = (IntPtr)(image + 1);
               
                using (var cpuContext = platformRenderInterface.CreateBackendContext(null))
                using (var renderTarget = cpuContext.CreateRenderTarget(new[] { this }))
                using (var ctx = renderTarget.CreateDrawingContext(true))
                {
                    var r = new Rect(_pixelSize.ToSize(1)); 
                    ctx.DrawBitmap(bitmap, 1, r, r);
                }

                Handle = XLib.XcursorImageLoadCursor(display, _blob.Address);
            }

            public string HandleDescriptor => "XCURSOR";

            public override void Dispose()
            {
                XLib.XFreeCursor(_display, Handle);
                _blob.Dispose();
            }

            public ILockedFramebuffer Lock()
            {
                return new LockedFramebuffer(
                    _blob.Address + Marshal.SizeOf<XcursorImage>(),
                    _pixelSize, _pixelSize.Width * 4,
                    new Vector(96, 96), PixelFormat.Bgra8888, null);
            }
            
            public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
        }
        
        private nint GetCursorHandleCached(StandardCursorType type)
        {
            if (!_cursors.TryGetValue(type, out var handle))
            {
                if(s_libraryCursors.TryGetValue(type, out var cursorName))
                    handle = XLib.XcursorLibraryLoadCursor(_display, cursorName);
                else if(s_mapping.TryGetValue(type, out var cursorShape))
                    handle = XLib.XCreateFontCursor(_display, cursorShape);

                if (handle == IntPtr.Zero)
                {
                    if (type != StandardCursorType.Arrow)
                        handle = GetCursorHandleCached(StandardCursorType.Arrow);
                    else
                        handle = _nullCursor;
                }
                
                _cursors[type] = handle;
            }

            return handle;
        }
    }

    internal class CursorImpl : ICursorImpl
    {
        public CursorImpl() { }
        public CursorImpl(IntPtr handle) => Handle = handle;
        public IntPtr Handle { get; protected set; }
        public virtual void Dispose() { }
    }
}
