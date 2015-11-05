using Perspex.Platform;

namespace Perspex.RenderHelpers
{
    static class QuadBezierHelper
    {
        public static void QuadTo(IStreamGeometryContextImpl context, Point current, Point controlPoint, Point endPoint)
        {
            //(s, (s + 2c)/ 3, (e + 2c)/ 3, e)
            context.BezierTo((current + 2*controlPoint)/3, (endPoint + 2*controlPoint)/3, endPoint);
        }
    }
}
