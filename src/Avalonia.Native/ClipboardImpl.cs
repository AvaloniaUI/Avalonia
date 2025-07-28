#nullable enable

using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal sealed class ClipboardImpl(IAvnClipboard native) : IOwnedClipboardImpl, IDisposable
    {
        private IAvnClipboard? _native = native;
        private long _lastClearChangeCount = long.MinValue;

        internal IAvnClipboard Native
            => _native ?? throw new ObjectDisposedException(nameof(ClipboardImpl));

        private void ClearCore()
        {
            _lastClearChangeCount = Native.Clear();
        }

        public Task ClearAsync()
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

        public Task<IDataTransfer?> TryGetDataAsync()
        {
            try
            {
                return Task.FromResult(TryGetData());
            }
            catch (Exception ex)
            {
                return Task.FromException<IDataTransfer?>(ex);
            }
        }

        private IDataTransfer? TryGetData()
        {
            var dataTransfer = new ClipboardDataTransfer(
                new ClipboardReadSession(Native, Native.ChangeCount, ownsNative: false));

            if (dataTransfer.Formats.Length == 0)
            {
                dataTransfer.Dispose();
                return null;
            }

            return dataTransfer;
        }

        public Task SetDataAsync(IDataTransfer dataTransfer)
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

        private void SetData(IDataTransfer dataTransfer)
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
