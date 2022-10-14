using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SdBitmap = System.Drawing.Bitmap;
using SdPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Avalonia.Win32
{
    internal class CursorFactory : ICursorFactory
    {
        public static CursorFactory Instance { get; } = new CursorFactory();

        private CursorFactory()
        {
        }

        static CursorFactory()
        {
            LoadModuleCursor(StandardCursorType.DragMove, "ole32.dll", 2);
            LoadModuleCursor(StandardCursorType.DragCopy, "ole32.dll", 3);
            LoadModuleCursor(StandardCursorType.DragLink, "ole32.dll", 4);
        }

        private static void LoadModuleCursor(StandardCursorType cursorType, string module, int id)
        {
            IntPtr mh = UnmanagedMethods.GetModuleHandle(module);
            if (mh != IntPtr.Zero)
            {
                IntPtr cursor = UnmanagedMethods.LoadCursor(mh, new IntPtr(id));
                if (cursor != IntPtr.Zero)
                {
                    Cache.Add(cursorType, new CursorImpl(cursor, false));
                }
            }
        }

        private static readonly Dictionary<StandardCursorType, int> CursorTypeMapping = new Dictionary
            <StandardCursorType, int>
        {
            {StandardCursorType.None, 0},
            {StandardCursorType.AppStarting, 32650},
            {StandardCursorType.Arrow, 32512},
            {StandardCursorType.Cross, 32515},
            {StandardCursorType.Hand, 32649},
            {StandardCursorType.Help, 32651},
            {StandardCursorType.Ibeam, 32513},
            {StandardCursorType.No, 32648},
            {StandardCursorType.SizeAll, 32646},
            {StandardCursorType.UpArrow, 32516},
            {StandardCursorType.SizeNorthSouth, 32645},
            {StandardCursorType.SizeWestEast, 32644},
            {StandardCursorType.Wait, 32514},
            //Same as SizeNorthSouth
            {StandardCursorType.TopSide, 32645},
            {StandardCursorType.BottomSide, 32645},
            //Same as SizeWestEast
            {StandardCursorType.LeftSide, 32644},
            {StandardCursorType.RightSide, 32644},
            //Using SizeNorthWestSouthEast
            {StandardCursorType.TopLeftCorner, 32642},
            {StandardCursorType.BottomRightCorner, 32642},
            //Using SizeNorthEastSouthWest
            {StandardCursorType.TopRightCorner, 32643},
            {StandardCursorType.BottomLeftCorner, 32643},

            // Fallback, should have been loaded from ole32.dll
            {StandardCursorType.DragMove, 32516},
            {StandardCursorType.DragCopy, 32516},
            {StandardCursorType.DragLink, 32516},
        };

        private static readonly Dictionary<StandardCursorType, CursorImpl> Cache =
            new Dictionary<StandardCursorType, CursorImpl>();

        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            if (!Cache.TryGetValue(cursorType, out var rv))
            {
                rv = new CursorImpl(
                    UnmanagedMethods.LoadCursor(IntPtr.Zero, new IntPtr(CursorTypeMapping[cursorType])),
                    false);
                Cache.Add(cursorType, rv);
            }

            return rv;
        }

        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            using var source = CursorFactory.LoadSystemDrawingBitmap(cursor);
            using var mask = AlphaToMask(source);

            var info = new UnmanagedMethods.ICONINFO
            {
                IsIcon = false,
                xHotspot = hotSpot.X,
                yHotspot = hotSpot.Y,
                MaskBitmap = mask.GetHbitmap(),
                ColorBitmap = source.GetHbitmap(),
            };

            return new CursorImpl(UnmanagedMethods.CreateIconIndirect(ref info), true);
        }

        private static SdBitmap LoadSystemDrawingBitmap(IBitmapImpl bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream);
            return new SdBitmap(memoryStream);
        }

        private unsafe SdBitmap AlphaToMask(SdBitmap source)
        {
            var dest = new SdBitmap(source.Width, source.Height, SdPixelFormat.Format1bppIndexed);

            if (source.PixelFormat == SdPixelFormat.Format32bppPArgb)
            {
                throw new NotSupportedException(
                    "Images with premultiplied alpha not yet supported as cursor images.");
            }

            if (source.PixelFormat != SdPixelFormat.Format32bppArgb)
            {
                return dest;
            }

            var sourceData = source.LockBits(
                new Rectangle(default, source.Size),
                ImageLockMode.ReadOnly,
                SdPixelFormat.Format32bppArgb);
            var destData = dest.LockBits(
                new Rectangle(default, source.Size),
                ImageLockMode.ReadOnly,
                SdPixelFormat.Format1bppIndexed);

            try
            {
                var pSource = (byte*)sourceData.Scan0.ToPointer();
                var pDest = (byte*)destData.Scan0.ToPointer();

                for (var y = 0; y < dest.Height; ++y)
                {
                    for (var x = 0; x < dest.Width; ++x)
                    {
                        if (pSource[x * 4] == 0)
                        {
                            pDest[x / 8] |= (byte)(1 << (x % 8));
                        }
                    }

                    pSource += sourceData.Stride;
                    pDest += destData.Stride;
                }

                return dest;
            }
            finally
            {
                source.UnlockBits(sourceData);
                dest.UnlockBits(destData);
            }
        }
    }

    internal class CursorImpl : ICursorImpl, IPlatformHandle
    {
        private readonly bool _isCustom;

        public CursorImpl(IntPtr handle, bool isCustom)
        {
            Handle = handle;
            _isCustom = isCustom;
        }

        public IntPtr Handle { get; private set; }
        public string HandleDescriptor => PlatformConstants.CursorHandleType;

        public void Dispose()
        {
            if (_isCustom && Handle != IntPtr.Zero)
            {
                UnmanagedMethods.DestroyIcon(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
