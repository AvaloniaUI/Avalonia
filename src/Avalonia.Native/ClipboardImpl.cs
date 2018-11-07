// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class ClipboardImpl : IClipboard
    {
        private IAvnClipboard _native;

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        public Task ClearAsync()
        {
            _native.Clear();

            return Task.CompletedTask;
        }

        public unsafe Task<string> GetTextAsync()
        {
            var text = _native.GetText();

            var result = System.Text.Encoding.UTF8.GetString((byte*)text.GetPointer(), text.GetLength());

            return Task.FromResult(result);
        }

        public Task SetTextAsync(string text)
        {
            _native.Clear();

            if (text != null)
            {
                _native.SetText(text);
            }

            return Task.CompletedTask;
        }
    }
}
