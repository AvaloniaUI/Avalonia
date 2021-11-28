using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;

namespace Avalonia.Blazor
{
    class ClipboardStub : IClipboard
    {
        public Task<string> GetTextAsync() => Task.FromResult("");

        public Task SetTextAsync(string text) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult<object>(null);
    }

    class CursorStub : ICursorImpl
    {
        public void Dispose()
        {
            
        }
    }

    class CursorFactoryStub : ICursorFactory
    {
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            return new CursorStub();
        }

        ICursorImpl ICursorFactory.GetCursor(StandardCursorType cursorType)
        {
            return new CursorStub();
        }
    }

    class IconLoaderStub : IPlatformIconLoader
    {
        class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {
                
            }
        }

        public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
    }

    class SystemDialogsStub : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent) =>
            Task.FromResult((string[]) null);

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent) =>
            Task.FromResult((string) null);
    }

    class ScreenStub : IScreenImpl
    {
        public int ScreenCount => 1;

        public IReadOnlyList<Screen> AllScreens { get; } =
            new Screen[] { new Screen(96, new PixelRect(0, 0, 4000, 4000), new PixelRect(0, 0, 4000, 4000), true) };
    }
}