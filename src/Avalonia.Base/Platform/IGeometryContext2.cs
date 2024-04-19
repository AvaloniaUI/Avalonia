namespace Avalonia.Platform
{
    // TODO12 combine with IGeometryContext
    public interface IGeometryContext2 : IGeometryContext
    {
        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="isStroked">Whether the segment is stroked</param>
        void LineTo(Point point, bool isStroked);
    }

}
