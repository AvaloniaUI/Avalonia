// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class CursorFactory : IStandardCursorFactory
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
                    PlatformHandle phCursor = new PlatformHandle(cursor, PlatformConstants.CursorHandleType);
                    Cache.Add(cursorType, phCursor);
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
            {StandardCursorType.BottomSize, 32645},
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

        private static readonly Dictionary<StandardCursorType, IPlatformHandle> Cache =
            new Dictionary<StandardCursorType, IPlatformHandle>();

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            IPlatformHandle rv;
            if (!Cache.TryGetValue(cursorType, out rv))
            {
                Cache[cursorType] =
                    rv =
                        new PlatformHandle(
                            UnmanagedMethods.LoadCursor(IntPtr.Zero, new IntPtr(CursorTypeMapping[cursorType])),
                            PlatformConstants.CursorHandleType);
            }

            return rv;
        }
    }
}
