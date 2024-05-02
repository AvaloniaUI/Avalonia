using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;

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
                    Cache.Add(cursorType, new CursorImpl(cursor));
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
                    UnmanagedMethods.LoadCursor(IntPtr.Zero, new IntPtr(CursorTypeMapping[cursorType])));
                Cache.Add(cursorType, rv);
            }

            return rv;
        }

        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            return new CursorImpl(new Win32Icon(cursor, hotSpot));
        }
    }

    internal class CursorImpl : ICursorImpl, IPlatformHandle
    {
        private Win32Icon? _icon;

        public CursorImpl(Win32Icon icon) : this(icon.Handle)
        {
            _icon = icon;
        }
        
        public CursorImpl(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; private set; }
        public string HandleDescriptor => PlatformConstants.CursorHandleType;

        public void Dispose()
        {
            if (_icon != null)
            {
                _icon.Dispose();
                _icon = null;
                Handle = IntPtr.Zero;
            }
        }
    }
}
