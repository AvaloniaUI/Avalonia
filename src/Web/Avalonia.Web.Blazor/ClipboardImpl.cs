using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor
{
    internal class ClipboardImpl : IClipboard
    {
        public async Task<string> GetTextAsync()
        {
            return await AvaloniaLocator.Current.GetRequiredService<IJSInProcessRuntime>().
                InvokeAsync<string>("navigator.clipboard.readText");
        }

        public async Task SetTextAsync(string text)
        {
            await AvaloniaLocator.Current.GetRequiredService<IJSInProcessRuntime>().
                InvokeAsync<string>("navigator.clipboard.writeText",text);
        }

        public Task ClearAsync() => Task.CompletedTask;

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult<object>(new());
    }
}
