using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
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

