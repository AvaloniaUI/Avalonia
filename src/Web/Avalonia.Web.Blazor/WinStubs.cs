using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Web.Blazor
{
    internal class ClipboardStub : IClipboard
    {
        public Task<string> GetTextAsync() => Task.FromResult("");

        public Task SetTextAsync(string text) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult<object>(new ());
    }

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

    internal class SystemDialogsStub : ISystemDialogImpl
    {
        public Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent) =>
            Task.FromResult((string[]?)null);

        public Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent) =>
            Task.FromResult((string?)null);
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
