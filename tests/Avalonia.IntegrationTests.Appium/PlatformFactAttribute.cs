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
        private string? _skip;

        public PlatformFactAttribute(TestPlatforms platforms, string? reason = null)
        {
            _reason = reason;
            Platforms = platforms;
        }

        public TestPlatforms Platforms { get; }

        public override string? Skip
        {
            get
            {
                if (_skip is not null)
                    return _skip;
                if (!IsSupported())
                    return $"Ignored on {RuntimeInformation.OSDescription}" +
                           (_reason is not null ? $" reason: '{_reason}'" : "");
                return null;
            }
            set => _skip = value;
        }

        private bool IsSupported()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platforms.HasFlag(TestPlatforms.Windows);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platforms.HasFlag(TestPlatforms.MacOS);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Platforms.HasFlag(TestPlatforms.Linux);
            return false;
        }
    }
}
