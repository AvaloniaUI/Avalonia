using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Media
{
    public interface IGlyphDrawing
    {
        Rect Bounds { get; }

        void Draw(DrawingContext context, Point origin);
    }
}
