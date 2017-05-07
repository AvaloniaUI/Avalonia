using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    class Direct2DChecker : IModuleEnvironmentChecker
    {
        //Direct2D backend doesn't work on some machines anymore
        public bool IsCompatible => true;
    }
}
