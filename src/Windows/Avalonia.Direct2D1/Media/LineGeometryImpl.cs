using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.LineGeometry"/>.
    /// </summary>
    internal class LineGeometryImpl : StreamGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public LineGeometryImpl(Point p1, Point p2)
        {
            using (var sink = ((PathGeometry)Geometry).Open())
            {
                sink.BeginFigure(p1.ToSharpDX(), FigureBegin.Hollow);
                sink.AddLine(p2.ToSharpDX());
                sink.EndFigure(FigureEnd.Open);
                sink.Close();
            }
        }
    }
}
