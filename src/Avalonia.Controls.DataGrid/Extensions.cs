using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    internal static class Extensions
    {
        internal static Point Translate(this Visual fromElement, Visual toElement, Point fromPoint)
        {
            if (fromElement == toElement)
            {
                return fromPoint;
            }
            else
            {
                var transform = fromElement.TransformToVisual(toElement);
                if (transform.HasValue)
                    return fromPoint.Transform(transform.Value);
                else
                    return fromPoint;
            }
        }
    }
}