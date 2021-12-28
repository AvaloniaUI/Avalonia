using System;

namespace Avalonia.Platform
{
    public interface IPlatformSettings
    {
        Size DoubleClickSize { get; }

        TimeSpan DoubleClickTime { get; }

        /// <summary>
        /// Determines the size of the area within that you should click twice in order for a double click to be counted.
        /// </summary>
        Size TouchDoubleClickSize { get; }

        /// <summary>
        /// Determines the time span that what will be used to determine the double-click.
        /// </summary>
        TimeSpan TouchDoubleClickTime { get; }
    }
}
