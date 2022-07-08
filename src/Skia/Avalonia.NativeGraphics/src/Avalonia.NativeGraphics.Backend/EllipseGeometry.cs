using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    internal class EllipseGeometry : GeometryImpl
    {
        public EllipseGeometry(IAvgFactory factory, Rect rect) : base(factory)
        {
            _avgPath.AddRect(rect.ToAvgRect());
        }
        
    }
}