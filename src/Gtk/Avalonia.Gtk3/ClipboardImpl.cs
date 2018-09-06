using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Gtk3.Interop;
using Avalonia.Input.Platform;

namespace Avalonia.Gtk3
{
    class ClipboardImpl : IClipboard
    {

        IntPtr GetClipboard() => Native.GtkClipboardGetForDisplay(Native.GdkGetDefaultDisplay(), IntPtr.Zero);

        static void OnText(IntPtr clipboard, IntPtr utf8string, IntPtr userdata)
        {
            var handle = GCHandle.FromIntPtr(userdata);

            ((TaskCompletionSource<string>) handle.Target)
                .TrySetResult(Utf8Buffer.StringFromPtr(utf8string));
            handle.Free();
        }

        private static readonly Native.D.GtkClipboardTextReceivedFunc OnTextDelegate = OnText;

        static ClipboardImpl()
        {
            GCHandle.Alloc(OnTextDelegate);
        }

        public Task<string> GetTextAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            Native.GtkClipboardRequestText(GetClipboard(), OnTextDelegate, GCHandle.ToIntPtr(GCHandle.Alloc(tcs)));
            return tcs.Task;
        }

        public Task SetTextAsync(string text)
        {
            using (var buf = new Utf8Buffer(text))
                Native.GtkClipboardSetText(GetClipboard(), buf, buf.ByteLen);
            return Task.FromResult(0);
        }

        public Task ClearAsync()
        {
            Native.GtkClipboardRequestClear(GetClipboard());
            return Task.FromResult(0);
        }
    }
}
