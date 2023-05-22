using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal class PlatformTheoryAttribute : TheoryAttribute
    {
        private string? _skip;

        public PlatformTheoryAttribute(TestPlatforms platforms = TestPlatforms.All) => Platforms = platforms;

        public TestPlatforms Platforms { get; }

        public override string? Skip
        {
            get
            {
                if (_skip is not null)
                    return _skip;
                return !IsSupported() ? $"Ignored on {RuntimeInformation.OSDescription}" : null;
            }
            set => _skip = value;
        }

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
