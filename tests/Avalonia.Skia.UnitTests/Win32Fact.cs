using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    internal class Win32Fact : FactAttribute
    {
        public Win32Fact(
            string message,
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Skip = message;
        }
    }
}
