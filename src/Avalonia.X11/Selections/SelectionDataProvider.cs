using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.X11.Selections.Clipboard;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Selections;

/// <summary>
/// Provides an X11 selection (clipboard/drag-and-drop) via a dedicated window.
/// </summary>
internal abstract class SelectionDataProvider : IDisposable
{
    private readonly IntPtr _selection;
    private readonly int _maximumPropertySize;

    protected AvaloniaX11Platform Platform { get; }

    public IAsyncDataTransfer? DataTransfer { get; protected set; }

    protected SelectionDataProvider(AvaloniaX11Platform platform, IntPtr selection)
    {
        Platform = platform;
        _selection = selection;

        var maxRequestSize = XExtendedMaxRequestSize(Platform.Display);
        if (maxRequestSize == 0)
            maxRequestSize = XMaxRequestSize(Platform.Display);

        _maximumPropertySize = (int)Math.Min(0x100000, maxRequestSize - 0x100);
    }

    protected IntPtr GetOwner()
        => XGetSelectionOwner(Platform.Display, _selection);

    protected void SetOwner(IntPtr owner)
        => XSetSelectionOwner(Platform.Display, _selection, owner, 0);

    public void OnSelectionRequest(in XSelectionRequestEvent request)
    {
        var response = new XEvent
        {
            SelectionEvent =
            {
                type = XEventName.SelectionNotify,
                send_event = 1,
                display = Platform.Display,
                selection = request.selection,
                target = request.target,
                requestor = request.requestor,
                time = request.time,
                property = 0
            }
        };

        if (request.selection == _selection)
        {
            response.SelectionEvent.property = WriteTargetToProperty(request.target, request.requestor, request.property);
        }

        XSendEvent(Platform.Display, request.requestor, false, new IntPtr((int)EventMask.NoEventMask), ref response);

        IntPtr WriteTargetToProperty(IntPtr target, IntPtr window, IntPtr property)
        {
            var atoms = Platform.Info.Atoms;

            if (target == atoms.TARGETS)
            {
                var atomValues = ConvertDataTransfer(DataTransfer);
                XChangeProperty(Platform.Display, window, property, atoms.ATOM, 32, PropertyMode.Replace,
                    atomValues, atomValues.Length);
                return property;
            }

            if (target == atoms.SAVE_TARGETS)
            {
                return property;
            }

            if (DataFormatHelper.ToDataFormat(target, atoms) is { } dataFormat)
            {
                if (DataTransfer is null)
                    return 0;

                // Our default bitmap format is image/png
                if (dataFormat.Identifier is "image/png" && DataTransfer.Contains(DataFormat.Bitmap))
                    dataFormat = DataFormat.Bitmap;

                if (!DataTransfer.Contains(dataFormat))
                    return 0;

                if (TryGetDataAsBytes(DataTransfer, dataFormat, target) is not { } bytes)
                    return 0;

                _ = SendDataToClientAsync(window, property, target, bytes);
                return property;
            }

            if (target == atoms.MULTIPLE)
            {
                XGetWindowProperty(Platform.Display, window, property, 0, int.MaxValue, false,
                    atoms.ATOM_PAIR, out _, out var actualFormat, out var nitems, out _, out var prop);

                if (nitems == 0)
                    return 0;

                if (actualFormat == 32)
                {
                    unsafe
                    {
                        var data = (IntPtr*)prop.ToPointer();
                        for (var c = 0; c < nitems.ToInt32(); c += 2)
                        {
                            var subTarget = data[c];
                            var subProp = data[c + 1];
                            var converted = WriteTargetToProperty(subTarget, window, subProp);
                            data[c + 1] = converted;
                        }

                        XChangeProperty(Platform.Display, window, property, atoms.ATOM_PAIR, 32, PropertyMode.Replace,
                            prop.ToPointer(), nitems.ToInt32());
                    }
                }

                XFree(prop);

                return property;
            }

            return 0;
        }

    }

    private byte[]? TryGetDataAsBytes(IAsyncDataTransfer dataTransfer, DataFormat format, IntPtr targetFormatAtom)
    {
        if (DataFormat.Text.Equals(format))
        {
            var text = dataTransfer.TryGetValueAsync(DataFormat.Text).GetAwaiter().GetResult();

            return DataFormatHelper.TryGetStringEncoding(targetFormatAtom, Platform.Info.Atoms) is { } encoding ?
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

            return UriListHelper.FileUriListToUtf8Bytes(files);
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

        using var events = new EventStreamWindow(Platform, window);
        using var _ = data;

        XSelectInput(Platform.Display, window, new IntPtr((int)XEventMask.PropertyChangeMask));

        var size = new IntPtr(data.Length);
        XChangeProperty(Platform.Display, window, property, Platform.Info.Atoms.INCR, 32, PropertyMode.Replace,
            ref size, 1);

        var buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(_maximumPropertySize, data.Length));

        while (true)
        {
            var evt = await events.WaitForEventAsync(
                x =>
                    x.type == XEventName.PropertyNotify &&
                    x.PropertyEvent.atom == property &&
                    x.PropertyEvent.state == 1,
                TimeSpan.FromMinutes(1));

            if (evt is null)
                break;

            var read = await data.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
                break;

            XChangeProperty(Platform.Display, window, property, target, 8, PropertyMode.Replace, buffer, read);
        }

        ArrayPool<byte>.Shared.Return(buffer);

        // Finish the transfer
        XChangeProperty(Platform.Display, window, property, target, 8, PropertyMode.Replace, 0, 0);
    }

    private Task SendDataToClientAsync(IntPtr window, IntPtr property, IntPtr target, byte[] bytes)
    {
        if (bytes.Length < _maximumPropertySize)
        {
            XChangeProperty(Platform.Display, window, property, target, 8, PropertyMode.Replace,
                bytes, bytes.Length);
            return Task.CompletedTask;
        }

        return SendIncrDataToClientAsync(window, property, target, new MemoryStream(bytes));
    }

    protected IntPtr[] ConvertDataTransfer(IAsyncDataTransfer? dataTransfer)
    {
        var atoms = Platform.Info.Atoms;

        var formatAtoms = DataFormatHelper.ToAtoms(dataTransfer?.Formats ?? [], atoms);
        return [atoms.TARGETS, atoms.MULTIPLE, ..formatAtoms];
    }

    public abstract void Dispose();
}
