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
}
