// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Input
{
    /*
    =========================================================================================
        NOTE: Cursors are NOT disposable and are cached in platform implementation.
        To support loading custom cursors some measures about that should be taken beforehand
    =========================================================================================
    */

    public enum StandardCursorType
    {
        Arrow,
        Ibeam,
        Wait,
        Cross,
        UpArrow,
        SizeWestEast,
        SizeNorthSouth,
        SizeAll,
        No,
        Hand,
        AppStarting,
        Help,
        TopSide,
        BottomSize,
        LeftSide,
        RightSide,
        TopLeftCorner,
        TopRightCorner,
        BottomLeftCorner,
        BottomRightCorner

        // Not available in GTK directly, see http://www.pixelbeat.org/programming/x_cursors/ 
        // We might enable them later, preferably, by loading pixmax direclty from theme with fallback image
        // SizeNorthWestSouthEast,
        // SizeNorthEastSouthWest,
    }

    public class Cursor
    {
        public static Cursor Default = new Cursor(StandardCursorType.Arrow);

        internal Cursor(IPlatformHandle platformCursor)
        {
            PlatformCursor = platformCursor;
        }

        public Cursor(StandardCursorType cursorType)
            : this(GetCursor(cursorType))
        {
        }

        public IPlatformHandle PlatformCursor { get; }

        private static IPlatformHandle GetCursor(StandardCursorType type)
        {
            var platform = AvaloniaLocator.Current.GetService<IStandardCursorFactory>();

            if (platform == null)
            {
                throw new Exception("Could not create Cursor: IStandardCursorFactory not registered.");
            }

            return platform.GetCursor(type);
        }
    }
}
