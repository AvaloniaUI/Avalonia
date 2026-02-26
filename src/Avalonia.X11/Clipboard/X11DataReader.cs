using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace Avalonia.X11.Clipboard;

    internal class X11DataReader(
    X11Info x11,
    AvaloniaX11Platform platform)
{
    private readonly X11Info _x11 = x11;
    private readonly AvaloniaX11Platform _platform = platform;

    public async Task<object?> TryGetAsync(DataFormat format, IntPtr formatAtom)
    {
        using var session = new ClipboardReadSession(_platform);
        var result = await session.SendDataRequest(formatAtom).ConfigureAwait(false);
        return ConvertDataResult(result, format, formatAtom);
    }

    private object? ConvertDataResult(ClipboardReadSession.GetDataResult? result, DataFormat format, IntPtr formatAtom)
    {
        if (result is null)
            return null;

        if (DataFormat.Text.Equals(format))
        {
            return ClipboardDataFormatHelper.TryGetStringEncoding(result.TypeAtom, _x11.Atoms) is { } textEncoding ?
                textEncoding.GetString(result.AsBytes()) :
                null;
        }

        if (DataFormat.Bitmap.Equals(format))
        {
            using var data = result.AsStream();

            return new Bitmap(data);
        }

        if (DataFormat.File.Equals(format))
        {
            // text/uri-list might not be supported
            return formatAtom != IntPtr.Zero && result.TypeAtom == formatAtom ?
                ClipboardUriListHelper.Utf8BytesToFileUriList(result.AsBytes()) :
                null;
        }

        if (format is DataFormat<string>)
            return Encoding.UTF8.GetString(result.AsBytes());

        if (format is DataFormat<byte[]>)
            return result.AsBytes();

        return null;
    }
}

