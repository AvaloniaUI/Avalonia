// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.Input
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
        Help

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
            : this(
                ((IStandardCursorFactory)PerspexLocator.Current.GetService(typeof(IStandardCursorFactory))).GetCursor(
                    cursorType))
        {
        }

        public IPlatformHandle PlatformCursor { get; }
    }
}
