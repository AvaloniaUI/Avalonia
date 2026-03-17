using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Selections.Clipboard;

internal sealed class X11ClipboardImpl(AvaloniaX11Platform platform)
    : SelectionDataProvider(platform, platform.Info.Atoms.CLIPBOARD), IOwnedClipboardImpl
{
    private TaskCompletionSource? _storeAtomTcs;

    protected override void OnEvent(ref XEvent ev)
    {
        if (ev.type == XEventName.SelectionClear)
        {
            // We night have already regained the clipboard ownership by the time a SelectionClear message arrives.
            if (GetOwner() != Window)
                DataTransfer = null;

            _storeAtomTcs?.TrySetResult();
        }

        else if (ev.type == XEventName.SelectionNotify)
        {
            var atoms = Platform.Info.Atoms;

            if (ev.SelectionEvent.selection == atoms.CLIPBOARD_MANAGER &&
                ev.SelectionEvent.target == atoms.SAVE_TARGETS)
            {
                _storeAtomTcs?.TrySetResult();
            }
        }

        else
            base.OnEvent(ref ev);
    }

    private SelectionReadSession OpenReadSession()
        => ClipboardReadSessionFactory.CreateSession(Platform);

    private Task StoreAtomsInClipboardManager(IAsyncDataTransfer dataTransfer)
    {
        var atoms = Platform.Info.Atoms;

        var clipboardManager = XGetSelectionOwner(Platform.Display, atoms.CLIPBOARD_MANAGER);
        if (clipboardManager == IntPtr.Zero)
            return Task.CompletedTask;

        // Skip storing atoms if the data object contains any non-trivial formats
        if (dataTransfer.Formats.Any(f => !DataFormat.Text.Equals(f)))
            return Task.CompletedTask;

        return StoreTextCoreAsync();

        async Task StoreTextCoreAsync()
        {
            // Skip storing atoms if the trivial formats are too big
            var text = await dataTransfer.TryGetTextAsync();
            if (text is null || text.Length * 2 > 64 * 1024)
                return;

            if (_storeAtomTcs is null || _storeAtomTcs.Task.IsCompleted)
                _storeAtomTcs = new TaskCompletionSource();

            var atomValues = ConvertDataTransfer(dataTransfer);

            XChangeProperty(Platform.Display, Window, atoms.AVALONIA_SAVE_TARGETS_PROPERTY_ATOM, atoms.ATOM, 32,
                PropertyMode.Replace, atomValues, atomValues.Length);

            XConvertSelection(Platform.Display, atoms.CLIPBOARD_MANAGER, atoms.SAVE_TARGETS,
                atoms.AVALONIA_SAVE_TARGETS_PROPERTY_ATOM, Window, 0);

            await _storeAtomTcs.Task;
        }
    }

    public Task ClearAsync()
    {
        DataTransfer = null;
        SetOwner(0);
        return Task.CompletedTask;
    }

    public async Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        var owner = GetOwner();
        if (owner == 0)
            return null;

        if (owner == Window && DataTransfer is { } storedDataTransfer)
            return storedDataTransfer;

        // Get the formats while we're in an async method, since IAsyncDataTransfer.GetFormats() is synchronous.
        var (dataFormats, textFormatAtoms) = await GetDataFormatsCoreAsync().ConfigureAwait(false);
        if (dataFormats.Length == 0)
            return null;

        // Get the items while we're in an async method. This does not get values, except for DataFormat.File.
        var reader = new ClipboardDataReader(Platform, textFormatAtoms, dataFormats, owner);
        var items = await reader.CreateItemsAsync();
        return new ClipboardDataTransfer(reader, dataFormats, items);
    }

    private async Task<(DataFormat[] DataFormats, IntPtr[] TextFormatAtoms)> GetDataFormatsCoreAsync()
    {
        using var session = OpenReadSession();

        var formatAtoms = await session.SendFormatRequest(0) ?? [];
        return DataFormatHelper.ToDataFormats(formatAtoms, Platform.Info.Atoms);
    }

    public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        DataTransfer = dataTransfer;
        SetOwner(Window);
        return StoreAtomsInClipboardManager(dataTransfer);
    }

    public Task<bool> IsCurrentOwnerAsync()
        => Task.FromResult(GetOwner() == Window);
}
