using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    internal class RectangleGeometry : GeometryImpl
    {
        public RectangleGeometry(IAvgFactory factory, Rect rect) : base(factory)
        {
            _avgPath.AddRect(rect.ToAvgRect());
        }
    }
}