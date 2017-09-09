namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Represents an image (e.g. a <see cref="Bitmap"/> or a <see cref="IDrawing"/>).
    /// </summary>
    public interface IImage
    {
        double Width { get; }

        double Height { get; }
    }
}