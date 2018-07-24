using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Monitor
    {
        public LogicalSize Size { get; set; }
        public LogicalPosition Position { get; set; }
    }
}
