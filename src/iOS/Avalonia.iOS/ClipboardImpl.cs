using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using UIKit;

namespace Avalonia.iOS
{
    internal class ClipboardImpl : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            return Task.FromResult(UIPasteboard.General.String);
        }

        public Task SetTextAsync(string text)
        {
            UIPasteboard.General.String = text;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            UIPasteboard.General.String = "";
            return Task.CompletedTask;
        }

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult<object>(null);
    }
}
