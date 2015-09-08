// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Gdk;
using Perspex.Input.Platform;

namespace Perspex.Gtk
{
    using Gtk = global::Gtk;

    internal class ClipboardImpl : IClipboard
    {
        private static Gtk.Clipboard GetClipboard() => Gtk.Clipboard.GetForDisplay(Display.Default, new Atom(IntPtr.Zero));

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
