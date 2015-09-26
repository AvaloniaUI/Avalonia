using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Input;

namespace Perspex.iOS
{
    class CursorFactory : IStandardCursorFactory
    {
        public static CursorFactory Instance { get; } = new CursorFactory();

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            throw new NotImplementedException();
        }
    }
}
