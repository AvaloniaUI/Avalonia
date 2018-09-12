using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            return Task.FromResult((string[])null);
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            return Task.FromResult<string>(null);
        }
    }

    class ClipboardImpl : IClipboard
    {
        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }

        public Task<string> GetTextAsync()
        {
            return Task.FromResult<string>(null);
        }

        public Task SetTextAsync(string text)
        {
            return Task.CompletedTask;
        }
    }

    class ScreenImpl : IScreenImpl
    {
        public int ScreenCount => 1;

        public Screen[] AllScreens => new[] { new Screen(new Rect(0, 0, 1600, 900), new Rect(0, 0, 1600, 900), true) };
    }
}
