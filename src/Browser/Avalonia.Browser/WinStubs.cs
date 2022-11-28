using System.Collections.Generic;
using System.IO;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Browser
{
    internal class IconLoaderStub : IPlatformIconLoader
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

    internal class ScreenStub : IScreenImpl
    {
        public int ScreenCount => 1;

        public IReadOnlyList<Screen> AllScreens { get; } =
            new[] { new Screen(96, new PixelRect(0, 0, 4000, 4000), new PixelRect(0, 0, 4000, 4000), true) };

        public Screen? ScreenFromPoint(PixelPoint point)
        {
            return ScreenHelper.ScreenFromPoint(point, AllScreens);
        }

        public Screen? ScreenFromRect(PixelRect rect)
        {
            return ScreenHelper.ScreenFromRect(rect, AllScreens);
        }

        public Screen? ScreenFromWindow(IWindowBaseImpl window)
        {
            return ScreenHelper.ScreenFromWindow(window, AllScreens);
        }
    }
}
