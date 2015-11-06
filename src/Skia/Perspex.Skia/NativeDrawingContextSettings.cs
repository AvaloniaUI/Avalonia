using System.Runtime.InteropServices;

namespace Perspex.Skia
{
    [StructLayout(LayoutKind.Sequential)]
    struct NativeDrawingContextSettings
    {
        public double Opacity;
    }
}