using System;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Input;
using Avalonia.Input.Platform;
using static Avalonia.Android.Platform.Specific.Helpers.ClipDataHelper;

namespace Avalonia.Android.Platform
{
    internal sealed class ClipboardImpl(ClipboardManager? clipboardManager, Context? context)
        : IClipboardImpl
    {
        private readonly ClipboardManager? _clipboardManager = clipboardManager;
        private readonly Context? _context = context;

        public Task<IAsyncDataTransfer?> TryGetDataAsync()
        {
            try
            {
                return Task.FromResult<IAsyncDataTransfer?>(TryGetData());
            }
            catch (Exception ex)
            {
                return Task.FromException<IAsyncDataTransfer?>(ex);
            }
        }

        private ClipDataToDataTransferWrapper? TryGetData()
            => _clipboardManager?.PrimaryClip is { } clipData ?
                new ClipDataToDataTransferWrapper(clipData, _context) :
                null;

        public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            if (_clipboardManager is null)
                return;

            var clipData = await GetClipData(dataTransfer, this);
            if (clipData is null)
            {
                Clear();
                return;
            }

            _clipboardManager.PrimaryClip = clipData;
        }

        public Task ClearAsync()
        {
            try
            {
                Clear();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        private void Clear()
        {
            if (_clipboardManager is null)
                return;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                _clipboardManager.ClearPrimaryClip();
            else
                _clipboardManager.PrimaryClip = ClipData.NewPlainText(null, string.Empty);
        }
    }
}
