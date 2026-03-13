using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// An object used to read values, converted to the correct format, from the X11 clipboard.
/// </summary>
internal sealed class ClipboardDataReader(
    X11Info x11,
    AvaloniaX11Platform platform,
    IntPtr[] textFormatAtoms,
    IntPtr owner,
    DataFormat[] dataFormats)
    : IDisposable
{
    private IntPtr _owner = owner;

    private bool IsOwnerStillValid()
        => _owner != IntPtr.Zero && XGetSelectionOwner(x11.Display, x11.Atoms.CLIPBOARD) == _owner;
    
    public async Task<object?> TryGetAsync(DataFormat format)
    {
        if (!IsOwnerStillValid())
            return null;

        var formatAtom = ClipboardDataFormatHelper.ToAtom(format, textFormatAtoms, x11.Atoms, dataFormats);
        if (formatAtom == IntPtr.Zero)
            return null;

        using var session = ClipboardReadSessionFactory.CreateSession(platform);
        var result = await session.SendDataRequest(formatAtom, 0).ConfigureAwait(false);
        return ConvertDataResult(result, format, formatAtom);
    }

    private object? ConvertDataResult(SelectionReadSession.GetDataResult? result, DataFormat format, IntPtr formatAtom)
    {
        if (result is null)
            return null;

        if (DataFormat.Text.Equals(format))
        {
            return ClipboardDataFormatHelper.TryGetStringEncoding(result.TypeAtom, x11.Atoms) is { } textEncoding ?
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

    public void Dispose()
        => _owner = IntPtr.Zero;
}
