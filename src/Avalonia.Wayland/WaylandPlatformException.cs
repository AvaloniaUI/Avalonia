using System;

namespace Avalonia.Wayland
{
    public class WaylandPlatformException : Exception
    {
        public WaylandPlatformException(string message) : base(message) { }
    }
}
