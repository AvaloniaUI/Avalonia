using System.Threading.Tasks;
using Avalonia.Input.Platform;
using UIKit;

namespace Avalonia.iOS
{
    public class Clipboard : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            return Task.FromResult(UIPasteboard.General.String);
        }

        public Task SetTextAsync(string text)
        {
            UIPasteboard.General.String = text;
            return Task.FromResult(0);
        }

        public Task ClearAsync()
        {
            UIPasteboard.General.String = "";
            return Task.FromResult(0);
        }
    }
}