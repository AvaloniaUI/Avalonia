using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;

//TODO: This file should be empty once everything is implemented

namespace Avalonia.Gtk3
{
    class ClipboardStub : IClipboard
    {
        public Task<string> GetTextAsync() => Task.FromResult("");

        public Task SetTextAsync(string text) => Task.FromResult(0);

        public Task ClearAsync() => Task.FromResult(0);
    }

    class CursorFactoryStub :  IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType) => new PlatformHandle(IntPtr.Zero, "STUB");
    }

    class SystemDialogStub : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent) => Task.FromResult(new string[0]);

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
            => Task.FromResult((string) null);
    }

    class PlatformIconLoaderStub : IPlatformIconLoader
    {
        public IWindowIconImpl LoadIcon(string fileName) => null;

        public IWindowIconImpl LoadIcon(Stream stream) => null;

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => null;
    }
}
