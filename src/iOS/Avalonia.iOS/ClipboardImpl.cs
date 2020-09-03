using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using UIKit;

namespace Avalonia.iOS
{
    public class ClipboardImpl : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            return Task.FromResult(UIPasteboard.General.String);
        }

        public Task SetTextAsync(string text)
        {
            UIPasteboard.General.String = text;
            return Task.FromResult(0);
        }

        public Task ClearAsync()
        {
            UIPasteboard.General.String = "";
            return Task.FromResult(0);
        }

        public Task SetDataObjectAsync(IDataObject data) => throw new PlatformNotSupportedException();

        public Task<string[]> GetFormatsAsync() => throw new PlatformNotSupportedException();

        public Task<object> GetDataAsync(string format) => throw new PlatformNotSupportedException();
    }
}