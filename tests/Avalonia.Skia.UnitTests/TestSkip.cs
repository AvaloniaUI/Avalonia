using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Skia.UnitTests
{   
    public class Win32Fact : FactAttribute
    {
        public Win32Fact(string message)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Skip = message;
        }
    }
}

