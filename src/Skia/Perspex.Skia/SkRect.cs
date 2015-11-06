using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Perspex.Skia
{
    [StructLayout(LayoutKind.Sequential)]
    struct SkRect
    {
        public float Left, Top, Right, Bottom;

        public static SkRect FromRect(Rect rc)
        {
            return new SkRect()
            {
                Left = (float) rc.X,
                Top = (float) rc.Y,
                Right = (float) rc.Right,
                Bottom = (float) rc.Bottom
            };
        }

        public Rect ToRect() => new Rect(Left, Top, Right - Left, Bottom - Top);
    }
}
