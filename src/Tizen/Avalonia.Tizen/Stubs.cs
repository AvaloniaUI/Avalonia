using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Tizen;

internal class WindowingPlatformStub : IWindowingPlatform
{
    public IWindowImpl CreateWindow() => throw new NotSupportedException();

    public IWindowImpl CreateEmbeddableWindow() => throw new NotSupportedException();

    public ITrayIconImpl? CreateTrayIcon() => null;
}

internal class PlatformIconLoaderStub : IPlatformIconLoader
{
    public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream);
            return LoadIcon(stream);
        }
    }

    public IWindowIconImpl LoadIcon(Stream stream)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new IconStub(ms);
    }

    public IWindowIconImpl LoadIcon(string fileName)
    {
        using (var file = File.Open(fileName, FileMode.Open))
            return LoadIcon(file);
    }
}

internal class IconStub : IWindowIconImpl
{
    private readonly MemoryStream _ms;

    public IconStub(MemoryStream stream)
    {
        _ms = stream;
    }

    public void Save(Stream outputStream)
    {
        _ms.Position = 0;
        _ms.CopyTo(outputStream);
    }
}

internal class CursorFactoryStub : ICursorFactory
{
    public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorImplStub();
    ICursorImpl ICursorFactory.GetCursor(StandardCursorType cursorType) => new CursorImplStub();

    private class CursorImplStub : ICursorImpl
    {
        public void Dispose() { }
    }
}
