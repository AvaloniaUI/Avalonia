using System.Collections.Generic;
using AndroidX.Core.View;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Android
{
    internal class CursorFactory : ICursorFactory
    {
        private Dictionary<StandardCursorType, ICursorImpl> _cache = [];
        public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot) => new CursorImpl(cursor, hotSpot);

        public ICursorImpl GetCursor(StandardCursorType cursorType) => GetOrCache(cursorType);

        public ICursorImpl GetOrCache(StandardCursorType cursorType)
        {
            if (_cache.TryGetValue(cursorType, out var cursor))
            {
                return cursor;
            }

            var newCursor = new CursorImpl(cursorType);
            _cache[cursorType] = newCursor;
            return newCursor;
        }
    }

    internal class CursorImpl : ICursorImpl
    {
        public static CursorImpl DefaultCursor { get; } = new CursorImpl(StandardCursorType.Arrow);
        public PointerIconCompat? PointerIcon { get; }

        public CursorImpl(Bitmap cursor, PixelPoint hotSpot)
        {
            using var memoryStream = new System.IO.MemoryStream();
            cursor.Save(memoryStream, new PngBitmapEncoderOptions());
            using var bitmap = global::Android.Graphics.BitmapFactory.DecodeByteArray(memoryStream.ToArray(), 0, (int)memoryStream.Length);
            PointerIcon = PointerIconCompat.Create(bitmap, hotSpot.X, hotSpot.Y);
        }

        public CursorImpl(StandardCursorType cursorType)
        {
            PointerIcon = PointerIconCompat.GetSystemIcon(global::Android.App.Application.Context, GetPointerIconType(cursorType));
        }

        private int GetPointerIconType(StandardCursorType cursorType) => cursorType switch
        {
            StandardCursorType.Arrow => PointerIconCompat.TypeArrow,
            StandardCursorType.Cross => PointerIconCompat.TypeCrosshair,
            StandardCursorType.Hand => PointerIconCompat.TypeHand,
            StandardCursorType.Ibeam => PointerIconCompat.TypeText,
            StandardCursorType.Wait or StandardCursorType.AppStarting => PointerIconCompat.TypeWait,
            StandardCursorType.Help => PointerIconCompat.TypeHelp,
            StandardCursorType.No => PointerIconCompat.TypeNoDrop,
            StandardCursorType.SizeAll => PointerIconCompat.TypeAllScroll,
            StandardCursorType.SizeNorthSouth or
                StandardCursorType.TopSide or
                StandardCursorType.BottomSide => PointerIconCompat.TypeVerticalDoubleArrow,
            StandardCursorType.SizeWestEast or
                StandardCursorType.LeftSide or
                StandardCursorType.RightSide => PointerIconCompat.TypeHorizontalDoubleArrow,
            StandardCursorType.TopLeftCorner => PointerIconCompat.TypeTopLeftDiagonalDoubleArrow,
            StandardCursorType.TopRightCorner => PointerIconCompat.TypeTopRightDiagonalDoubleArrow,
            StandardCursorType.BottomLeftCorner => PointerIconCompat.TypeTopRightDiagonalDoubleArrow,
            StandardCursorType.BottomRightCorner => PointerIconCompat.TypeTopLeftDiagonalDoubleArrow,
            StandardCursorType.DragMove => PointerIconCompat.TypeGrabbing,
            StandardCursorType.DragCopy => PointerIconCompat.TypeCopy,
            StandardCursorType.DragLink => PointerIconCompat.TypeAlias,
            StandardCursorType.None => PointerIconCompat.TypeNull,
            _ => PointerIconCompat.TypeArrow,
        };

        public void Dispose() { }
    }
}
