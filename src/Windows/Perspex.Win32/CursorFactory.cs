// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Input;
using Perspex.Platform;

namespace Perspex.Win32
{
    internal class CursorFactory : IStandardCursorFactory
    {
        public static CursorFactory Instance { get; } = new CursorFactory();

        private CursorFactory()
        {
        }

        private static readonly Dictionary<StandardCursorType, int> CursorTypeMapping = new Dictionary
            <StandardCursorType, int>
        {
            { StandardCursorType.AppStarting, 32650 },
            { StandardCursorType.Arrow, 32512 },
            { StandardCursorType.Cross, 32515 },
            { StandardCursorType.Hand, 32649 },
            { StandardCursorType.Help, 32651 },
            { StandardCursorType.Ibeam, 32513 },
            { StandardCursorType.No, 32648 },
            { StandardCursorType.SizeAll, 32646 },

            // { StandardCursorType.SizeNorthEastSouthWest, 32643 },
            { StandardCursorType.SizeNorthSouth, 32645 },

            // { StandardCursorType.SizeNorthWestSouthEast, 32642 },
            { StandardCursorType.SizeWestEast, 32644 },
            { StandardCursorType.UpArrow, 32516 },
            { StandardCursorType.Wait, 32514 }
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
