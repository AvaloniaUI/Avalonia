using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class AvaloniaNativeCursor : ICursorImpl, IDisposable
    {
        public IAvnCursor? Cursor { get; private set; }
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "<none>";

        public AvaloniaNativeCursor(IAvnCursor cursor)
        {
            Cursor = cursor;
        }

        public void Dispose()
        {
            Cursor?.Dispose();
            Cursor = null;
        }
    }

    class CursorFactory : ICursorFactory
    {
        IAvnCursorFactory _native;

        public CursorFactory(IAvnCursorFactory native)
        {
            _native = native;
        }

        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            var cursor = _native.GetCursor((AvnStandardCursorType)cursorType);
            return new AvaloniaNativeCursor( cursor );
        }

        public unsafe ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
        {
            using(var ms = new MemoryStream())
            {
                cursor.Save(ms, PngBitmapEncoderOptions.Default);

                var imageData = ms.ToArray();

                // Points are DIPs on macOS, so the cursor takes the bitmap's DPI into account: a 64x64 bitmap
                // at 192 DPI shows as a 32x32 cursor that is sharp on Retina displays.
                var size = cursor.Size;
                var hotSpotPoint = hotSpot.ToPointWithDpi(cursor.Dpi);

                fixed(void* ptr = imageData)
                {
                    var avnCursor = _native.CreateCustomCursor(ptr, new IntPtr(imageData.Length),
                        new AvnSize { Width = size.Width, Height = size.Height },
                        new AvnPoint { X = hotSpotPoint.X, Y = hotSpotPoint.Y });

                    return new AvaloniaNativeCursor(avnCursor);
                }
            }
        }
    }
}
