using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{ 
    class ClipboardImpl : IClipboard
    {
        IAvnClipboard _native;

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }

        public Task<string> GetTextAsync()
        {
            var outPtr = _native.GetText();
            return Task.FromResult(Marshal.PtrToStringAnsi(outPtr));
        }

        public Task SetTextAsync(string text)
        {
            return Task.CompletedTask;
        }
    }
}
