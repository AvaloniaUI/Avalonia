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
        private const int OleRetryCount = 10;
        private const int OleRetryDelay = 100;

        private async Task<IDisposable> OpenClipboard()
        {
            var i = OleRetryCount;
            var delay = 100;

            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                if (--i == 0)
                    throw new TimeoutException("Timeout opening clipboard.");
                await Task.Delay(delay);
                delay += 100;
            }

            return Disposable.Create(() => UnmanagedMethods.CloseClipboard());
        }

        public async Task<string> GetTextAsync()
        {
            try
            {
                using (await OpenClipboard())
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
            catch (TimeoutException)
            {
                return "CAN'T OPEN THE CLIPBOARD. PLS TRY AGAIN.";
            }
        }

        public async Task SetTextAsync(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            try
            {
                using (await OpenClipboard())
                {
                    UnmanagedMethods.EmptyClipboard();

                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("COULD NOT OPEN CLIPBOARD. COULDN'T SET TEXT");
                Console.WriteLine("--BEGINNING OF TEXT--");
                Console.WriteLine(text);
                Console.WriteLine("--END OF TEXT--");
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                using (await OpenClipboard())
                {
                    UnmanagedMethods.EmptyClipboard();
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("COULD NOT OPEN CLIPBOARD. IT WASN'T CLEARED");
            }
        }

        public async Task SetDataObjectAsync(IDataObject data)
        {
            Dispatcher.UIThread.VerifyAccess();
            var wrapper = new DataObject(data);
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleSetClipboard(wrapper);

                if (hr == 0)
                    break;

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);
                
                await Task.Delay(OleRetryDelay);
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);

                if (hr == 0)
                {
                    var wrapper = new OleDataObject(dataObject);
                    var formats = wrapper.GetDataFormats().ToArray();
                    Marshal.ReleaseComObject(dataObject);
                    return formats;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        public async Task<object> GetDataAsync(string format)
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);

                if (hr == 0)
                {
                    var wrapper = new OleDataObject(dataObject);
                    var rv = wrapper.Get(format);
                    Marshal.ReleaseComObject(dataObject);
                    return rv;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }
    }
}
