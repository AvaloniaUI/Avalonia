using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LogicalPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LogicalSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
