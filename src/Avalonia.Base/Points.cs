using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia
{
    public sealed class Points : AvaloniaList<Point>
    {
        public Points()
        {
            
        }

        public Points(IEnumerable<Point> points) : base(points)
        {
            
        }
    }
}
