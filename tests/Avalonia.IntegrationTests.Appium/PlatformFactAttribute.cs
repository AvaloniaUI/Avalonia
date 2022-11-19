#nullable enable
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia
{
    [Flags]
    internal enum TestPlatforms
    {
        Windows = 0x01,
        MacOS = 0x02,
        Linux = 0x04,
        All = Windows | MacOS | Linux,
    }

    internal class PlatformFactAttribute : FactAttribute
    {
        private readonly string? _reason;

        public PlatformFactAttribute(TestPlatforms platforms, string? reason = null)
        {
            _reason = reason;
            Platforms = platforms;
        }

        public TestPlatforms Platforms { get; }

        public override string? Skip
        {
            get => IsSupported() ? null : $"Ignored on {RuntimeInformation.OSDescription}" + (_reason is not null ? $" reason: \"{_reason}\"" : "");
            set => throw new NotSupportedException();
        }

        private bool IsSupported()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platforms.HasAnyFlag(TestPlatforms.Windows);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platforms.HasAnyFlag(TestPlatforms.MacOS);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Platforms.HasAnyFlag(TestPlatforms.Linux);
            return false;
        }
    }
}
