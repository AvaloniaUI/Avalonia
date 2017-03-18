using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    class WindowsVersionChecker : IModuleEnvironmentChecker
    {
        //Direct2D backend doesn't work with Win7 anymore
        public bool IsCompatible => Environment.OSVersion.Version >= new Version(6, 2);
    }
}
