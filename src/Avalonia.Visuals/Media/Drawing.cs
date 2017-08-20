namespace Avalonia.Media
{
    public abstract class Drawing : AvaloniaObject
    {
        public abstract void Draw(DrawingContext context);

        public abstract Rect GetBounds();
    }
}