using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// An object used to read values, converted to the correct format, from the X11 clipboard.
/// </summary>
internal sealed class ClipboardDataReader(
    X11Info x11,
    AvaloniaX11Platform platform,
    IntPtr[] textFormatAtoms,
    IntPtr owner)
    : IDisposable
{
    private readonly X11Info _x11 = x11;
    private readonly AvaloniaX11Platform _platform = platform;
    private readonly IntPtr[] _textFormatAtoms = textFormatAtoms;
    private IntPtr _owner = owner;

    private bool IsOwnerStillValid()
        => _owner != IntPtr.Zero && XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD) == _owner;
    
    public async Task<object?> TryGetAsync(DataFormat format)
    {
        if (!IsOwnerStillValid())
            return null;

        var formatAtom = ClipboardDataFormatHelper.ToAtom(format, _textFormatAtoms, _x11.Atoms);
        if (formatAtom == IntPtr.Zero)
            return null;

        using var session = new ClipboardReadSession(_platform);
        var result = await session.SendDataRequest(formatAtom).ConfigureAwait(false);

        var fileAtom = format.Equals(DataFormat.File) ? formatAtom : IntPtr.Zero;
        return ConvertDataResult(result, fileAtom);
    }

    private object? ConvertDataResult(ClipboardReadSession.GetDataResult? result, IntPtr fileListAtom)
    {
        if (result is null)
            return null;

        if (ClipboardDataFormatHelper.TryGetStringEncoding(result.TypeAtom, _x11.Atoms) is { } textEncoding)
            return textEncoding.GetString(result.AsBytes());

        if (result.TypeAtom == fileListAtom && fileListAtom != IntPtr.Zero)
        {
            using var memoryStream = new MemoryStream(result.AsBytes());
            return ClipboardUriListHelper.TryReadFileUriList(memoryStream);
        }

        return result.AsBytes();
    }

    public void Dispose()
        => _owner = IntPtr.Zero;
}
