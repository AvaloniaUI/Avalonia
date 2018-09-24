using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Platform;

namespace Avalonia.Native
{ 
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
}
