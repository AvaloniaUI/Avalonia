using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia;

/// <summary>
/// Represents a collection of <see cref="Point"/> values that can be individually accessed by index.
/// </summary>
public sealed class Points : AvaloniaList<Point>
{
    public Points()
    {
        
    }

    public Points(IEnumerable<Point> points) : base(points)
    {
        
    }
}
