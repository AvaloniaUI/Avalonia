using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{

    /// <summary>
    /// Allows customization of hit-testing
    /// </summary>
    public interface ICustomHitTest
    {
        /// <param name="point">The point to hit test in global coordinate space.</param>
        bool HitTest(Point point);
    }
}
