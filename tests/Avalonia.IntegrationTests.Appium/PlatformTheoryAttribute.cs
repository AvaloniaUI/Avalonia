using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal class PlatformTheoryAttribute : TheoryAttribute
    {
        public PlatformTheoryAttribute(TestPlatforms platforms = TestPlatforms.All) => Platforms = platforms;

        public TestPlatforms Platforms { get; }

        public override string? Skip
        {
            get => IsSupported() ? null : $"Ignored on {RuntimeInformation.OSDescription}";
            set => throw new NotSupportedException();
        }

        private bool IsSupported()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platforms.HasAnyFlag(TestPlatforms.Windows);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platforms.HasAnyFlag(TestPlatforms.MacOS);
            return false;
        }
    }
}
