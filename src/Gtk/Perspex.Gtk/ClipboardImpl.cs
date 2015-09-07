namespace Perspex.Gtk
{
    using Gdk;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Input.Platform;
    using Gtk = global::Gtk;
    class ClipboardImpl : IClipboard
    {
        private static Gtk.Clipboard GetClipboard() => Gtk.Clipboard.GetForDisplay(Gdk.Display.Default, new Atom(IntPtr.Zero));

        public Task<string> GetTextAsync()
        {
            var clip = GetClipboard();
            var tcs = new TaskCompletionSource<string>();
            clip.RequestText((_, text) =>
            {
                tcs.TrySetResult(text);
            });
            return tcs.Task;
        }

        public Task SetTextAsync(string text)
        {
            using (var cl = GetClipboard())
                cl.Text = text;
            return Task.FromResult(0);
        }

        public Task ClearAsync()
        {
            using (var cl = GetClipboard())
                cl.Clear();
            return Task.FromResult(0);
        }
    }
}
