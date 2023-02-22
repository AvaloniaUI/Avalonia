using System;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser
{
    internal class ClipboardImpl : IClipboard
    {
        public Task<string?> GetTextAsync()
        {
            return InputHelper.ReadClipboardTextAsync()!;
        }

        public Task SetTextAsync(string? text)
        {
            return InputHelper.WriteClipboardTextAsync(text ?? string.Empty);
        }

        public async Task ClearAsync() => await SetTextAsync("");

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object?> GetDataAsync(string format) => Task.FromResult<object?>(null);
    }
}
