using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Input;
using Perspex.Platform;

namespace Perspex.Android
{
    class CursorFactory : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            throw new NotImplementedException();
        }
    }
}