using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal class PlatformTheoryAttribute : TheoryAttribute
    {
        public PlatformTheoryAttribute(
            TestPlatforms platforms = TestPlatforms.All,
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
        {
            Platforms = platforms;
            if (!IsSupported())
            {
                Skip = $"Ignored on {RuntimeInformation.OSDescription}";
            }
        }

        public TestPlatforms Platforms { get; }

        private bool IsSupported()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platforms.HasFlag(TestPlatforms.Windows);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platforms.HasFlag(TestPlatforms.MacOS);
            return false;
        }
    }
}
