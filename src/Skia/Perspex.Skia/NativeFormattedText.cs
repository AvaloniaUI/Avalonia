using System.Runtime.InteropServices;

/* No longer needed with SkiaSharp

namespace Perspex.Skia
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NativeFormattedText
    {
        public float WidthConstraint;
        public int LineCount;
        public NativeFormattedTextLine* Lines;
        public SkRect* Bounds;
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NativeFormattedTextLine
    {
        public float Top;
        public int Start;
        public int Length;
        public float Height;
        public float Width;
    };
}

*/