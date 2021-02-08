using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class AvaloniaNativeCursor : ICursorImpl, IDisposable
    {
        public IAvnCursor Cursor { get; private set; }
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "<none>";

        public AvaloniaNativeCursor(IAvnCursor cursor)
        {
            Cursor = cursor;
        }

        public void Dispose()
        {
            Cursor.Dispose();
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

        public unsafe ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            using(var ms = new MemoryStream())
            {
                cursor.Save(ms);

                var imageData = ms.ToArray();

                fixed(void* ptr = imageData)
                {
                    var avnCursor = _native.CreateCustomCursor(ptr, new IntPtr(imageData.Length),
                        new AvnPixelSize { Width = hotSpot.X, Height = hotSpot.Y });

                    return new AvaloniaNativeCursor(avnCursor);
                }
            }
        }
    }
}
