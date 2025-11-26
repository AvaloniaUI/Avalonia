using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard
{
    internal sealed class X11ClipboardImpl : IOwnedClipboardImpl
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly X11Info _x11;
        private IAsyncDataTransfer? _storedDataTransfer;
        private readonly IntPtr _handle;
        private TaskCompletionSource<bool>? _storeAtomTcs;
        private readonly IntPtr[] _textAtoms;
        private readonly IntPtr _avaloniaSaveTargetsAtom;
        private readonly int _maximumPropertySize;

        public X11ClipboardImpl(AvaloniaX11Platform platform)
        {
            _platform = platform;
            _x11 = platform.Info;
            _handle = CreateEventWindow(platform, OnEvent);
            _avaloniaSaveTargetsAtom = XInternAtom(_x11.Display, "AVALONIA_SAVE_TARGETS_PROPERTY_ATOM", false);
            _textAtoms = new[]
            {
                _x11.Atoms.XA_STRING,
                _x11.Atoms.OEMTEXT,
                _x11.Atoms.UTF8_STRING,
                _x11.Atoms.UTF16_STRING
            }.Where(a => a != IntPtr.Zero).ToArray();

            var extendedMaxRequestSize = XExtendedMaxRequestSize(_platform.Display);
            var maxRequestSize = XMaxRequestSize(_platform.Display);
            _maximumPropertySize =
                (int)Math.Min(0x100000, (extendedMaxRequestSize == IntPtr.Zero
                    ? maxRequestSize
                    : extendedMaxRequestSize).ToInt64() - 0x100);
        }
        
        private unsafe void OnEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.SelectionClear)
            {
                // We night have already regained the clipboard ownership by the time a SelectionClear message arrives.
                if (GetOwner() != _handle)
                    _storedDataTransfer = null;

                _storeAtomTcs?.TrySetResult(true);
                return;
            }

            if (ev.type == XEventName.SelectionRequest)
            {
                var sel = ev.SelectionRequestEvent;
                var resp = new XEvent
                {
                    SelectionEvent =
                    {
                        type = XEventName.SelectionNotify,
                        send_event = 1,
                        display = _x11.Display,
                        selection = sel.selection,
                        target = sel.target,
                        requestor = sel.requestor,
                        time = sel.time,
                        property = IntPtr.Zero
                    }
                };
                if (sel.selection == _x11.Atoms.CLIPBOARD)
                {
                    resp.SelectionEvent.property = WriteTargetToProperty(sel.target, sel.requestor, sel.property);
                }

                XSendEvent(_x11.Display, sel.requestor, false, new IntPtr((int)EventMask.NoEventMask), ref resp);
            }

            IntPtr WriteTargetToProperty(IntPtr target, IntPtr window, IntPtr property)
            {
                if (target == _x11.Atoms.TARGETS)
                {
                    var atoms = ConvertDataTransfer(_storedDataTransfer);
                    XChangeProperty(_x11.Display, window, property,
                        _x11.Atoms.XA_ATOM, 32, PropertyMode.Replace, atoms, atoms.Length);
                    return property;
                }
                else if (target == _x11.Atoms.SAVE_TARGETS && _x11.Atoms.SAVE_TARGETS != IntPtr.Zero)
                {
                    return property;
                }
                else if (ClipboardDataFormatHelper.ToDataFormat(target, _x11.Atoms) is { } dataFormat)
                {
                    if (_storedDataTransfer is null)
                        return IntPtr.Zero;

                    // Our default bitmap format is image/png
                    if (dataFormat.Identifier is "image/png" && _storedDataTransfer.Contains(DataFormat.Bitmap))
                        dataFormat = DataFormat.Bitmap;

                    if (!_storedDataTransfer.Contains(dataFormat))
                        return IntPtr.Zero;

                    if (TryGetDataAsBytes(_storedDataTransfer, dataFormat, target) is not { } bytes)
                        return IntPtr.Zero;

                    _ = SendDataToClientAsync(window, property, target, bytes);
                    return property;
                }
                else if (target == _x11.Atoms.MULTIPLE && _x11.Atoms.MULTIPLE != IntPtr.Zero)
                {
                    XGetWindowProperty(_x11.Display, window, property, IntPtr.Zero, new IntPtr(0x7fffffff), false,
                        _x11.Atoms.ATOM_PAIR, out _, out var actualFormat, out var nitems, out _, out var prop);
                    if (nitems == IntPtr.Zero)
                        return IntPtr.Zero;
                    if (actualFormat == 32)
                    {
                        var data = (IntPtr*)prop.ToPointer();
                        for (var c = 0; c < nitems.ToInt32(); c += 2)
                        {
                            var subTarget = data[c];
                            var subProp = data[c + 1];
                            var converted = WriteTargetToProperty(subTarget, window, subProp);
                            data[c + 1] = converted;
                        }

                        XChangeProperty(_x11.Display, window, property, _x11.Atoms.ATOM_PAIR, 32, PropertyMode.Replace,
                            prop.ToPointer(), nitems.ToInt32());
                    }

                    XFree(prop);

                    return property;
                }
                else
                    return IntPtr.Zero;
            }

        }

        private byte[]? TryGetDataAsBytes(IAsyncDataTransfer dataTransfer, DataFormat format, IntPtr targetFormatAtom)
        {
            if (DataFormat.Text.Equals(format))
            {
                var text = dataTransfer.TryGetValueAsync(DataFormat.Text).GetAwaiter().GetResult();

                return ClipboardDataFormatHelper.TryGetStringEncoding(targetFormatAtom, _x11.Atoms) is { } encoding ?
                    encoding.GetBytes(text ?? string.Empty) :
                    null;
            }

            if (DataFormat.Bitmap.Equals(format))
            {
                if (dataTransfer.TryGetValueAsync(DataFormat.Bitmap).GetAwaiter().GetResult() is not { } bitmap)
                    return null;

                using var stream = new MemoryStream();
                bitmap.Save(stream);

                return stream.ToArray();
            }
            
            if (DataFormat.File.Equals(format))
            {
                if (dataTransfer.TryGetValuesAsync(DataFormat.File).GetAwaiter().GetResult() is not { } files)
                    return null;

                return ClipboardUriListHelper.FileUriListToUtf8Bytes(files);
            }

            if (format is DataFormat<string> stringFormat)
            {
                return dataTransfer.TryGetValueAsync(stringFormat).GetAwaiter().GetResult() is { } stringValue ?
                    Encoding.UTF8.GetBytes(stringValue) :
                    null;
            }

            if (format is DataFormat<byte[]> bytesFormat)
                return dataTransfer.TryGetValueAsync(bytesFormat).GetAwaiter().GetResult();

            Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)
                ?.Log(this, "Unsupported data format {Format}", format);

            return null;
        }

        private async Task SendIncrDataToClientAsync(IntPtr window, IntPtr property, IntPtr target, Stream data)
        {
            data.Position = 0;
            using var events = new EventStreamWindow(_platform, window);
            using var _ = data;
            XSelectInput(_x11.Display, window, new IntPtr((int)XEventMask.PropertyChangeMask));
            var size = new IntPtr(data.Length);
            XChangeProperty(_x11.Display, window, property, _x11.Atoms.INCR, 32, PropertyMode.Replace, ref size, 1);
            var buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(_maximumPropertySize, data.Length));
            while (true)
            {
                if (null == await events.WaitForEventAsync(x =>
                        x.type == XEventName.PropertyNotify && x.PropertyEvent.atom == property
                                                            && x.PropertyEvent.state == 1, TimeSpan.FromMinutes(1)))
                    break;
                var read = await data.ReadAsync(buffer, 0, buffer.Length);
                if(read == 0)
                    break;
                XChangeProperty(_x11.Display, window, property, target, 8, PropertyMode.Replace, buffer, read);
            }
            ArrayPool<byte>.Shared.Return(buffer);

            // Finish the transfer
            XChangeProperty(_x11.Display, window, property, target, 8, PropertyMode.Replace, IntPtr.Zero, 0);
        }

        private Task SendDataToClientAsync(IntPtr window, IntPtr property, IntPtr target, byte[] bytes)
        {
            if (bytes.Length < _maximumPropertySize)
            {
                XChangeProperty(_x11.Display, window, property, target, 8,
                    PropertyMode.Replace,
                    bytes, bytes.Length);
                return Task.CompletedTask;
            }

            return SendIncrDataToClientAsync(window, property, target, new MemoryStream(bytes));
        }

        private IntPtr GetOwner()
            => XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD);
        
        private ClipboardReadSession OpenReadSession() => new(_platform);

        private IntPtr[] ConvertDataTransfer(IAsyncDataTransfer? dataTransfer)
        {
            var atoms = new List<IntPtr> { _x11.Atoms.TARGETS, _x11.Atoms.MULTIPLE };

            if (dataTransfer is not null)
            {
                foreach (var format in dataTransfer.Formats)
                {
                    foreach (var atom in ClipboardDataFormatHelper.ToAtoms(format, _textAtoms, _x11.Atoms))
                        atoms.Add(atom);
                }
            }

            return atoms.ToArray();
        }

        private Task StoreAtomsInClipboardManager(IAsyncDataTransfer dataTransfer)
        {
            if (_x11.Atoms.CLIPBOARD_MANAGER == IntPtr.Zero || _x11.Atoms.SAVE_TARGETS == IntPtr.Zero)
                return Task.CompletedTask;

            var clipboardManager = XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER);
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
                    _storeAtomTcs = new TaskCompletionSource<bool>();

                var atoms = ConvertDataTransfer(dataTransfer);
                XChangeProperty(_x11.Display, _handle, _avaloniaSaveTargetsAtom, _x11.Atoms.XA_ATOM, 32,
                    PropertyMode.Replace, atoms, atoms.Length);
                XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER, _x11.Atoms.SAVE_TARGETS,
                    _avaloniaSaveTargetsAtom, _handle, IntPtr.Zero);
                await _storeAtomTcs.Task;
            }
        }

        public Task ClearAsync()
        {
            _storedDataTransfer = null;
            XSetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD, IntPtr.Zero, IntPtr.Zero);
            return Task.CompletedTask;
        }

        public async Task<IAsyncDataTransfer?> TryGetDataAsync()
        {
            var owner = GetOwner();
            if (owner == IntPtr.Zero)
                return null;

            if (owner == _handle && _storedDataTransfer is { } storedDataTransfer)
                return storedDataTransfer;

            // Get the formats while we're in an async method, since IAsyncDataTransfer.GetFormats() is synchronous.
            var (dataFormats, textFormatAtoms) = await GetDataFormatsCoreAsync().ConfigureAwait(false);
            if (dataFormats.Length == 0)
                return null;

            // Get the items while we're in an async method. This does not get values, except for DataFormat.File.
            var reader = new ClipboardDataReader(_x11, _platform, textFormatAtoms, owner, dataFormats);
            var items = await CreateItemsAsync(reader, dataFormats);
            return new ClipboardDataTransfer(reader, dataFormats, items);
        }

        private async Task<(DataFormat[] DataFormats, IntPtr[] TextFormatAtoms)> GetDataFormatsCoreAsync()
        {
            using var session = OpenReadSession();

            var formatAtoms = await session.SendFormatRequest();
            if (formatAtoms is null)
                return ([], []);

            var formats = new List<DataFormat>(formatAtoms.Length);
            List<IntPtr>? textFormatAtoms = null;

            var hasImage = false;

            foreach (var formatAtom in formatAtoms)
            {
                if (ClipboardDataFormatHelper.ToDataFormat(formatAtom, _x11.Atoms) is not { } format)
                    continue;

                if (DataFormat.Text.Equals(format))
                {
                    if (textFormatAtoms is null)
                    {
                        formats.Add(format);
                        textFormatAtoms = [];
                    }
                    textFormatAtoms.Add(formatAtom);
                }
                else
                {
                    formats.Add(format);

                    if(!hasImage)
                    {
                        if (format.Identifier is ClipboardDataFormatHelper.JpegFormatMimeType or ClipboardDataFormatHelper.PngFormatMimeType)
                            hasImage = true;
                    }
                }
            }

            if (hasImage)
                formats.Add(DataFormat.Bitmap);

            return (formats.ToArray(), textFormatAtoms?.ToArray() ?? []);
        }

        private static async Task<IAsyncDataTransferItem[]> CreateItemsAsync(ClipboardDataReader reader, DataFormat[] formats)
        {
            List<DataFormat>? nonFileFormats = null;
            var items = new List<IAsyncDataTransferItem>();
            var hasFiles = false;

            foreach (var format in formats)
            {
                if (DataFormat.File.Equals(format))
                {
                    if (hasFiles)
                        continue;

                    // We're reading the filenames ahead of time to generate the appropriate items.
                    // This is async, so it should be fine.
                    if (await reader.TryGetAsync(format) is IEnumerable<IStorageItem> storageItems)
                    {
                        hasFiles = true;

                        foreach (var storageItem in storageItems)
                            items.Add(PlatformDataTransferItem.Create(DataFormat.File, storageItem));
                    }
                }
                else
                    (nonFileFormats ??= new()).Add(format);
            }

            // Single item containing all formats except for DataFormat.File.
            if (nonFileFormats is not null)
                items.Add(new ClipboardDataTransferItem(reader, formats));

            return items.ToArray();
        }

        public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            _storedDataTransfer = dataTransfer;
            XSetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD, _handle, IntPtr.Zero);
            return StoreAtomsInClipboardManager(dataTransfer);
        }

        public Task<bool> IsCurrentOwnerAsync()
            => Task.FromResult(GetOwner() == _handle);
    }
}
