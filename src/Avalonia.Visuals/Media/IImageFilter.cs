namespace Avalonia.Media
{
    public interface IImageFilter
    {
        bool Equals(IImageFilter other);
    }

    public interface IMutableImageFilter
    {
        IImageFilter ToImmutable();
    }

    public interface IBoundsAffectingImageFilter : IImageFilter
    {
        Rect UpdateBounds(Rect bounds);
    }
}
