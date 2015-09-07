namespace Perspex.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Input;

    public interface IStandardCursorFactory
    {
        IPlatformHandle GetCursor(StandardCursorType cursorType);
    }
}
