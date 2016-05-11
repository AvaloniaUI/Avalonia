using Avalonia.Platform;

namespace Avalonia.RenderHelpers
{
    static class QuadBezierHelper
    {
        public static void QuadraticBezierTo(IStreamGeometryContextImpl context, Point current, Point controlPoint, Point endPoint)
        {
            //(s, (s + 2c)/ 3, (e + 2c)/ 3, e)
            context.CubicBezierTo((current + 2*controlPoint)/3, (endPoint + 2*controlPoint)/3, endPoint);
        }
    }
}
