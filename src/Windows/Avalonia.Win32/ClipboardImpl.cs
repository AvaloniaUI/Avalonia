using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class ClipboardImpl : IClipboard
    {
        private async Task<IDisposable> OpenClipboard()
        {
            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                await Task.Delay(100);
            }

            return Disposable.Create(() => UnmanagedMethods.CloseClipboard());
        }

        public async Task<string> GetTextAsync()
        {
            using(await OpenClipboard())
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
        }

        public async Task SetTextAsync(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            using(await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();

                var hGlobal = Marshal.StringToHGlobalUni(text);
                UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
            }
        }

        public async Task ClearAsync()
        {
            using(await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();
            }
        }

        public async Task SetDataObjectAsync(IDataObject data)
        {
            Dispatcher.UIThread.VerifyAccess();
            var wrapper = new DataObject(data);
            while (true)
            {
                if (UnmanagedMethods.OleSetClipboard(wrapper) == 0)
                    break;
                await Task.Delay(100);
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            while (true)
            {
                if (UnmanagedMethods.OleGetClipboard(out var dataObject) == 0)
                {
                    var wrapper = new OleDataObject(dataObject);
                    var formats = wrapper.GetDataFormats().ToArray();
                    Marshal.ReleaseComObject(dataObject);
                    return formats;
                }

                await Task.Delay(100);
            }
        }

        public async Task<object> GetDataAsync(string format)
        {
            Dispatcher.UIThread.VerifyAccess();
            while (true)
            {
                if (UnmanagedMethods.OleGetClipboard(out var dataObject) == 0)
                {
                    var wrapper = new OleDataObject(dataObject);
                    var rv = wrapper.Get(format);
                    Marshal.ReleaseComObject(dataObject);
                    return rv;
                }

                await Task.Delay(100);
            }
        }
    }
}
