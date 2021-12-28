using System;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class PlatformSettingsStub : IPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(2, 2);
        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);

        /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickSize"/>
        public Size TouchDoubleClickSize => new Size(16, 16);

        /// <inheritdoc cref="IPlatformSettings.TouchDoubleClickTime"/>
        public TimeSpan TouchDoubleClickTime => DoubleClickTime;
    }
}
