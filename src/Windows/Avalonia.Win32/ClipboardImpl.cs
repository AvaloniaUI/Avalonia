using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal class ClipboardImpl : IClipboard
    {
        private const int OleRetryCount = 10;
        private const int OleRetryDelay = 100;

        private static async Task<IDisposable> OpenClipboard()
        {
            var i = OleRetryCount;

            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                if (--i == 0)
                    throw new TimeoutException("Timeout opening clipboard.");
                await Task.Delay(OleRetryDelay)
                    .ConfigureAwait(false);
            }

            return Disposable.Create(() => UnmanagedMethods.CloseClipboard());
        }

        public async Task<string?> GetTextAsync()
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

        public async Task SetTextAsync(string? text)
        {
            using (await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();

                if (text is not null)
                {
                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
                }
            }
        }

        public async Task ClearAsync()
        {
            using (await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();
            }
        }

        public async Task SetDataObjectAsync(IDataObject data)
        {
            Dispatcher.UIThread.VerifyAccess();
            using var wrapper = new DataObject(data);
            var i = OleRetryCount;

            var uiSchedulerContext = TaskScheduler.FromCurrentSynchronizationContext();

            var ptr = wrapper.GetNativeIntPtr<Win32Com.IDataObject>();

            while (true)
            {
                var hr = await Task.Factory.StartNew(SetDataObject,
                    state: ptr,
                    cancellationToken: default,
                    creationOptions: TaskCreationOptions.None
                    , uiSchedulerContext);

                if (hr == 0)
                    break;

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay)
                    .ConfigureAwait(false);
            }

            static int SetDataObject(object? state) =>
                UnmanagedMethods.OleSetClipboard((IntPtr)state!);
        }

        public async Task<string[]> GetFormatsAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            var uiSchedulerContext = TaskScheduler.FromCurrentSynchronizationContext();
            var i = OleRetryCount;

            while (true)
            {

                var state = await Task.Factory.StartNew(OleGetClipboard,
                    cancellationToken: default,
                    creationOptions: TaskCreationOptions.None,
                    uiSchedulerContext);

                if (state.hr == 0)
                {
                    return await Task.Factory.StartNew(GetDataFormats,
                        state: state.dataObjectPtr,
                        cancellationToken: default,
                        creationOptions: TaskCreationOptions.None,
                        uiSchedulerContext);
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(state.hr);

                await Task.Delay(OleRetryDelay)
                    .ConfigureAwait(false);
            }

            static (int hr, IntPtr dataObjectPtr) OleGetClipboard()
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);
                return (hr, dataObject);
            }

            static string[] GetDataFormats(object? state)
            {
                var dataObject = (IntPtr)state!;
                using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(dataObject, true);
                using var wrapper = new OleDataObject(proxy);
                var formats = wrapper.GetDataFormats().ToArray();
                return formats;
            }
        }

        public async Task<object?> GetDataAsync(string format)
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;
            var uiSchedulerContext = TaskScheduler.FromCurrentSynchronizationContext();

            while (true)
            {
                var state = await Task.Factory.StartNew(OleGetClipboard,
                    cancellationToken: default,
                    creationOptions: TaskCreationOptions.None,
                    uiSchedulerContext);

                if (state.hr == 0)
                {
                    return await Task.Factory.StartNew(GetData,
                        state: (state.dataObjectPtr, format),
                        cancellationToken: default,
                        creationOptions: TaskCreationOptions.None,
                        uiSchedulerContext);
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(state.hr);

                await Task.Delay(OleRetryDelay)
                    .ConfigureAwait(false);
            }

            static (int hr, IntPtr dataObjectPtr) OleGetClipboard()
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);
                return (hr, dataObject);
            }

            static object? GetData(object? state)
            {
                var info = (ValueTuple<IntPtr, string>)state!;
                using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(info.Item1, true);
                using var wrapper = new OleDataObject(proxy);
                var rv = wrapper.Get(info.Item2);
                return rv;
            }
        }
    }
}
