





namespace Perspex.Platform
{
    using System;

    public interface IPlatformSettings
    {
        Size DoubleClickSize { get; }

        TimeSpan DoubleClickTime { get; }
    }
}
