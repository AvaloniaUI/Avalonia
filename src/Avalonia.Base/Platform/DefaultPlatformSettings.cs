using System;
using Avalonia.Input;

namespace Avalonia.Platform
{
    /// <summary>
    /// A default implementation of <see cref="IPlatformSettings"/> for platforms which don't have
    /// an OS-specific implementation.
    /// </summary>
    public class DefaultPlatformSettings : IPlatformSettings
    {
        public Size GetTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(10, 10),
                _ => new(4, 4),
            };
        }
        public Size GetDoubleTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(16, 16),
                _ => new(4, 4),
            };
        }
        public TimeSpan GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(500);
    }
}
