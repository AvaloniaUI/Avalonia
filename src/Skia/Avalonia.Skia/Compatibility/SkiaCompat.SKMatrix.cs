using System.Runtime.InteropServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    // SkiaSharp 3.0 only:
    // https://github.com/mono/skia/blob/83c17a6dee5af2db80af57197627f6fbbe4ad272/include/c/sk_types.h#L177C1-L182C17
    [StructLayout(LayoutKind.Sequential)]
    public struct sk_matrix44_t {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;
    }

    public static sk_matrix44_t ToSkMatrix44(SKMatrix matrix)
    {
        return new sk_matrix44_t
        {
            m00 = matrix.ScaleX,
            m01 = matrix.SkewX,
            m02 = 0.0f,
            m03 = matrix.TransX,
            m10 = matrix.SkewY,
            m11 = matrix.ScaleY,
            m12 = 0.0f,
            m13 = matrix.TransY,
            m20 = 0.0f,
            m21 = 0.0f,
            m22 = 1f,
            m23 = 0.0f,
            m30 = matrix.Persp0,
            m31 = matrix.Persp1,
            m32 = 0.0f,
            m33 = matrix.Persp2
        };
    }
}
