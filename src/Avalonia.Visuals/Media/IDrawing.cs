using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    public interface IDrawing : IImage
    {
        void Draw(DrawingContext context);

        Rect GetBounds();
    }

    public interface IMutableDrawing : IDrawing
    {
        IDrawing ToImmutable();
    }
}