using Perspex.Input.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.iOS
{
    class ClipboardImpl : IClipboard
    {
        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTextAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetTextAsync(string text)
        {
            throw new NotImplementedException();
        }
    }
}
