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
            _native.Clear();
            return Task.CompletedTask;
        }

        public Task<string> GetTextAsync()
        {
            var outPtr = _native.GetText();
            var text = Marshal.PtrToStringAnsi(outPtr);
            return Task.FromResult(text);
        }

        public Task SetTextAsync(string text)
        {
            _native.Clear();
            if(text != null)
                _native.SetText(text);
            return Task.CompletedTask;
        }
    }
}
