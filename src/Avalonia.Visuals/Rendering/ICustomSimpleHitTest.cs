using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// An interface to allow non-templated controls to customize their hit-testing
    /// when using a renderer with a simple hit-testing algorithm without a scene graph,
    /// such as <see cref="ImmediateRenderer" />
    /// </summary>
    public interface ICustomSimpleHitTest
    {
        bool HitTest(Point point);
    }

    public static class CustomSimpleHitTestExtensions
    {
        public static bool HitTestCustom(this IVisual visual, Point point)
            => (visual as ICustomSimpleHitTest)?.HitTest(point) ?? visual.TransformedBounds?.Contains(point) == true;

        public static bool HitTestCustom(this IEnumerable<IVisual> children, Point point)
            => children.Any(ctrl => ctrl.HitTestCustom(point));
    }
}
