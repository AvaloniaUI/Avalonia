namespace Avalonia.Media
{
    public abstract class PathSegment : AvaloniaObject
    {
        internal abstract void ApplyTo(StreamGeometryContext ctx);
    }
}
