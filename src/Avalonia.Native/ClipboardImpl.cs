#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal sealed class ClipboardImpl : IOwnedClipboardImpl, IDisposable
    {
        private IAvnClipboard? _native;
        private long _lastClearChangeCount;

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        internal IAvnClipboard Native
            => _native ?? throw new ObjectDisposedException(nameof(ClipboardImpl));

        private void ClearCore()
        {
            _lastClearChangeCount = Native.Clear();
        }

        Task IClipboardImpl.ClearAsync()
        {
            try
            {
                ClearCore();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        Task<DataFormat[]> IClipboardImpl.GetDataFormatsAsync()
        {
            try
            {
                return Task.FromResult(GetFormats(out _));
            }
            catch (Exception ex)
            {
                return Task.FromException<DataFormat[]>(ex);
            }
        }

        private DataFormat[] GetFormats(out long changeCount)
        {
            changeCount = Native.ChangeCount;

            do
            {
                try
                {
                    using var formats = Native.GetFormats(changeCount);
                    return ClipboardDataFormatHelper.ToDataFormats(formats);
                }
                catch (COMException ex) when (ClipboardReadSession.IsComObjectDisposedException(ex))
                {
                    // The native side returns COR_E_OBJECTDISPOSED if the clipboard has changed (ChangeCount doesn't match).
                    // In that case, simply ignore the exception and retry.
                    var newChangeCount = Native.ChangeCount;
                    if (newChangeCount != changeCount)
                        changeCount = newChangeCount;
                    else
                        throw new ObjectDisposedException(nameof(ClipboardImpl), ex);
                }
            } while (true);
        }

        Task<IDataTransfer?> IClipboardImpl.TryGetDataAsync(IEnumerable<DataFormat> formats)
        {
            try
            {
                return Task.FromResult(TryGetData(formats));
            }
            catch (Exception ex)
            {
                return Task.FromException<IDataTransfer?>(ex);
            }
        }

        private IDataTransfer? TryGetData(IEnumerable<DataFormat> formats)
        {
            var currentFormats = GetFormats(out var changeCount);
            if (currentFormats.Length == 0)
                return null;

            foreach (var format in formats)
            {
                if (Array.IndexOf(currentFormats, format) >= 0)
                    return new ClipboardDataTransfer(new ClipboardReadSession(Native, changeCount, ownsNative: false));
            }

            return null;
        }

        Task IClipboardImpl.SetDataAsync(IDataTransfer dataTransfer)
        {
            try
            {
                SetData(dataTransfer);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public void SetData(IDataTransfer dataTransfer)
        {
            ClearCore();

            Native.SetData(new DataTransferToAvnClipboardDataSourceWrapper(dataTransfer));
        }

        public Task<bool> IsCurrentOwnerAsync()
            => Task.FromResult(Native.ChangeCount == _lastClearChangeCount);

        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }
    }
}
