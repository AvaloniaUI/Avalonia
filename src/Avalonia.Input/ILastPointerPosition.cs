namespace Avalonia.Input
{
    public interface ILastPointerPosition
    {
        PixelPoint? LastPointerPosition { get; }
        void SetLastPointerPosition(PixelPoint point);
    }
}
