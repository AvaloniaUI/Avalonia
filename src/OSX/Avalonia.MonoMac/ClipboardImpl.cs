using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class ClipboardImpl : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            return Task.FromResult(NSPasteboard.GeneralPasteboard.GetStringForType(NSPasteboard.NSStringType));
        }

        public Task SetTextAsync(string text)
        {
            NSPasteboard.GeneralPasteboard.ClearContents();
            if (text != null)
                NSPasteboard.GeneralPasteboard.SetStringForType(text, NSPasteboard.NSStringType);
            return Task.CompletedTask;
        }

        public async Task ClearAsync()
        {
            NSPasteboard.GeneralPasteboard.ClearContents();
        }
    }
}

