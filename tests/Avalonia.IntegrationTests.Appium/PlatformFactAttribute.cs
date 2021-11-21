using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal class PlatformFactAttribute : FactAttribute
    {
        public override string? Skip
        {
            get
            {
                if (SkipOnWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "Ignored on Windows";
                if (SkipOnOSX && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "Ignored on MacOS";
                return null;
            }
            set => throw new NotSupportedException();
        }
        public bool SkipOnOSX { get; set; }
        public bool SkipOnWindows { get; set; }
    }
}
