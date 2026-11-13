using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    internal class MacOSFact : FactAttribute
    {
        public MacOSFact(
            string message,
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Skip = message;
        }
    }
}
