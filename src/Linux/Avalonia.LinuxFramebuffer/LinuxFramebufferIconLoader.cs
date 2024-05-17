using System.IO;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer;

internal class LinuxFramebufferIconLoaderStub : IPlatformIconLoader
{
    private class IconStub : IWindowIconImpl
    {
        public void Save(Stream outputStream)
        {

        }
    }

    public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

    public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

    public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
}

