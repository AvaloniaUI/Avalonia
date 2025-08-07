using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal sealed class ClipboardImpl : IOwnedClipboardImpl, IFlushableClipboardImpl
    {
        private const int OleRetryCount = 10;
        private const int OleRetryDelay = 100;

        private DataTransferToOleDataObjectWrapper? _lastStoredDataObject;
        // We can't currently rely on GetNativeIntPtr due to a bug in MicroCom 0.11, so we store the raw CCW reference instead
        private IntPtr _lastStoredDataObjectIntPtr;

        /// <summary>
        /// The amount of time in milliseconds to sleep before flushing the clipboard after a set.
        /// </summary>
        /// <remarks>
        /// This is mitigation for clipboard listener issues.
        /// </remarks>
        private const int OleFlushDelay = 10;

        private static async Task<IDisposable> OpenClipboardAsync()
        {
            var i = OleRetryCount;

            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                if (--i == 0)
                    throw new TimeoutException("Timeout opening clipboard.");
                await Task.Delay(100);
            }

            return Disposable.Create(() => UnmanagedMethods.CloseClipboard());
        }

        public async Task ClearAsync()
        {
            using (await OpenClipboardAsync())
            {
                UnmanagedMethods.EmptyClipboard();
                ClearLastStoredObject();
            }
        }

        private void ClearLastStoredObject()
        {
            _lastStoredDataObject = null;
            _lastStoredDataObjectIntPtr = IntPtr.Zero;
        }

        public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            Dispatcher.UIThread.VerifyAccess();

            using var wrapper = new DataTransferToOleDataObjectWrapper(
                dataTransfer.ToSynchronous(LogArea.Win32Platform));
            var i = OleRetryCount;

            while (true)
            {
                var ptr = wrapper.GetNativeIntPtr<Win32Com.IDataObject>();
                var hr = UnmanagedMethods.OleSetClipboard(ptr);

                if (hr == 0)
                {
                    _lastStoredDataObject = wrapper;
                    // TODO: Replace with GetNativeIntPtr in TryGetInProcessDataObjectAsync
                    // once MicroCom is fixed
                    _lastStoredDataObjectIntPtr = ptr;
                    wrapper.OnDestroyed += delegate
                    {
                        if (_lastStoredDataObjectIntPtr == ptr)
                            ClearLastStoredObject();
                    };
                    break;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        public async Task<IAsyncDataTransfer?> TryGetDataAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);

                if (hr == 0)
                {
                    using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(dataObject, true);
                    var wrapper = new OleDataObjectToDataTransferWrapper(proxy);

                    if (wrapper.Formats.Length == 0)
                    {
                        wrapper.Dispose();
                        return null;
                    }

                    return wrapper;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        public Task<bool> IsCurrentOwnerAsync()
        {
            var isCurrent =
                _lastStoredDataObject is { IsDisposed: false } &&
                _lastStoredDataObjectIntPtr != IntPtr.Zero &&
                UnmanagedMethods.OleIsCurrentClipboard(_lastStoredDataObjectIntPtr) == (int)UnmanagedMethods.HRESULT.S_OK;

            if (!isCurrent)
                ClearLastStoredObject();

            return Task.FromResult(isCurrent);
        }

        public async Task FlushAsync()
        {
            await Task.Delay(OleFlushDelay);

            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

            int i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleFlushClipboard();

                if (hr == 0)
                {
                    break;
                }

                if (--i == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                await Task.Delay(OleRetryDelay);
            }
        }
    }
}
