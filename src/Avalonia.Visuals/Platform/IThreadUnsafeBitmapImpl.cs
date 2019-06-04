namespace Avalonia.Platform
{
    public interface IThreadUnsafeBitmapImpl : IBitmapImpl
    {
        IBitmapImpl Snapshot();
    }
}
