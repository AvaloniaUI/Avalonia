using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Skia.RenderTests
{   
    public class Win32Fact : FactAttribute
    {
        public Win32Fact(string message)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Skip = message;
        }
    }

    public class Win32Theory : TheoryAttribute
    {
        public Win32Theory(string message)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Skip = message;
        }
    }
}

