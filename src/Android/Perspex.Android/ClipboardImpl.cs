using System;
using System.Threading.Tasks;
using Perspex.Input.Platform;

namespace Perspex.Android
{
    internal class ClipboardImpl : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetTextAsync(string text)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }
    }
}