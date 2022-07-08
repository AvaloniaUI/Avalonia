using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    internal class LineGeometry : GeometryImpl
    {
        public LineGeometry(IAvgFactory factory, Point p1, Point p2) : base(factory)
        {
            _avgPath.MoveTo(p1.ToAvgPoint());
            _avgPath.LineTo(p2.ToAvgPoint());
        }
        
    }
}