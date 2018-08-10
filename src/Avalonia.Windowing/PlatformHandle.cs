using System;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class DummyPlatformHandle : IPlatformHandle
    {
        public IntPtr Handle => IntPtr.Zero;
        public string HandleDescriptor => "Dummy";
    }
}
