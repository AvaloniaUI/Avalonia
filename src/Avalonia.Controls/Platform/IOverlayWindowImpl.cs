using System;

namespace Avalonia.Platform
{
    public interface IOverlayWindowImpl
    {
        Action<string> FirstResponderChanged { get; set; }

        Func<Point, bool> ShouldPassThrough { get; set; }
    }
}
