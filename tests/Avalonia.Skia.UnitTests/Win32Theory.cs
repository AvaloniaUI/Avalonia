using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    internal class Win32Theory: TheoryAttribute
    {
        public Win32Theory(string message)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Skip = message;
        }
    }
}
