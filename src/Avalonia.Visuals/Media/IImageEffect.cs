namespace Avalonia.Media
{
    public interface IImageEffect
    {
        bool Equals(IImageEffect other);
    }

    public interface IMutableImageEffect
    {
        IImageEffect ToImmutable();
    }

    public interface IBoundsAffectingImageEffect : IImageEffect
    {
        Rect UpdateBounds(Rect bounds);
    }
}
