using Perspex.Win32.Interop;

namespace Perspex.Win32
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Input;
    using Perspex.Platform;

    class CursorFactory : IStandardCursorFactory
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
