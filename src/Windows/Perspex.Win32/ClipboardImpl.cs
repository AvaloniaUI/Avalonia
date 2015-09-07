namespace Perspex.Win32
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Input.Platform;
    using Perspex.Win32.Interop;
    using System.Runtime.InteropServices;

    class ClipboardImpl : IClipboard
    {
        async Task OpenClipboard()
        {
            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                await Task.Delay(100);
            }
        }

        public async Task<string> GetTextAsync()
        {
            await this.OpenClipboard();
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

            await this.OpenClipboard();
            try
            {
                var hGlobal  = Marshal.StringToHGlobalUni(text);
                UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
            }
            finally
            {
                UnmanagedMethods.CloseClipboard();
            }
        }

        public async Task ClearAsync()
        {
            await this.OpenClipboard();
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
