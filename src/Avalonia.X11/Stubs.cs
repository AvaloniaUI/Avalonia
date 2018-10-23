using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class CursorFactoryStub : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new PlatformHandle(IntPtr.Zero, "FAKE");
        }
    }

    class ClipboardStub : IClipboard
    {
        private string _text;
        public Task<string> GetTextAsync()
        {
            return Task.FromResult(_text);
        }

        public Task SetTextAsync(string text)
        {
            _text = text;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _text = null;
            return Task.CompletedTask;
        }
    }

    class PlatformSettingsStub : IPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(2, 2);
        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);
    }

    class SystemDialogsStub : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            return Task.FromResult((string[])null);
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            return Task.FromResult((string)null);
        }
    }

    class IconLoaderStub : IPlatformIconLoader
    {
        class FakeIcon : IWindowIconImpl
        {
            private readonly byte[] _data;

            public FakeIcon(byte[] data)
            {
                _data = data;
            }
            public void Save(Stream outputStream)
            {
                outputStream.Write(_data, 0, _data.Length);
            }
        }
        
        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new FakeIcon(File.ReadAllBytes(fileName));
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return new FakeIcon(ms.ToArray());
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms);
            return new FakeIcon(ms.ToArray());
        }
    }

    class ScreenStub : IScreenImpl
    {
        public int ScreenCount { get; } = 1;

        public Screen[] AllScreens { get; } =
            {new Screen(new Rect(0, 0, 1920, 1280), new Rect(0, 0, 1920, 1280), true)};
    }
}
