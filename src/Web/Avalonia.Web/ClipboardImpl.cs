using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Web.Blazor
{
    internal class ClipboardImpl : IClipboard
    {
        public async Task<string> GetTextAsync()
        {
            throw new NotImplementedException();
            //return await AvaloniaLocator.Current.GetRequiredService<IJSInProcessRuntime>().
              //  InvokeAsync<string>("navigator.clipboard.readText");
        }

        public async Task SetTextAsync(string text)
        {
            throw new NotImplementedException();
            //await AvaloniaLocator.Current.GetRequiredService<IJSInProcessRuntime>().
              //  InvokeAsync<string>("navigator.clipboard.writeText",text);
        }

        public async Task ClearAsync() => await SetTextAsync("");

        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult<object>(new());
    }
}
