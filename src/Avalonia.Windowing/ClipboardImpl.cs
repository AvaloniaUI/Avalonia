using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace Avalonia.Windowing
{
    public class ClipboardImpl : IClipboard
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_clipboard_get_text();

        [DllImport("winit_wrapper")]
        private static extern void winit_free_string(IntPtr stringPtr);

        [DllImport("winit_wrapper")]
        private static extern void winit_clipboard_set_text(IntPtr cstringPtr);

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }

        public Task<string> GetTextAsync()
        {
            var textPtr = winit_clipboard_get_text();

            var result = Marshal.PtrToStringAnsi(textPtr);

            winit_free_string(textPtr);

            return Task.FromResult<string>(result);
        }

        public Task SetTextAsync(string text)
        {
            var ptr = Marshal.StringToHGlobalAnsi(text);

            winit_clipboard_set_text(ptr);

            Marshal.FreeHGlobal(ptr);

            return Task.CompletedTask;
        }
    }
}
