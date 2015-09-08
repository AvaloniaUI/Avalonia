// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Input.Platform;
using Perspex.Win32.Interop;
using System.Runtime.InteropServices;

namespace Perspex.Win32
{
    internal class ClipboardImpl : IClipboard
    {
        private async Task OpenClipboard()
        {
            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                await Task.Delay(100);
            }
        }

        public async Task<string> GetTextAsync()
        {
            await OpenClipboard();
            try
            {
                IntPtr hText = UnmanagedMethods.GetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT);
                if (hText == IntPtr.Zero)
                {
                    return null;
                }

                var pText = UnmanagedMethods.GlobalLock(hText);
                if (pText == IntPtr.Zero)
                {
                    return null;
                }

                var rv = Marshal.PtrToStringUni(pText);
                UnmanagedMethods.GlobalUnlock(hText);
                return rv;
            }
            finally
            {
                UnmanagedMethods.CloseClipboard();
            }
        }

        public async Task SetTextAsync(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            await OpenClipboard();
            try
            {
                var hGlobal = Marshal.StringToHGlobalUni(text);
                UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
            }
            finally
            {
                UnmanagedMethods.CloseClipboard();
            }
        }

        public async Task ClearAsync()
        {
            await OpenClipboard();
            try
            {
                UnmanagedMethods.EmptyClipboard();
            }
            finally
            {
                UnmanagedMethods.CloseClipboard();
            }
        }
    }
}
