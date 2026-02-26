using System;
using System.IO;
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
    : X11DataReader(x11, platform), IDisposable
{
    private readonly X11Info _x11 = x11;
    private readonly IntPtr[] _textFormatAtoms = textFormatAtoms;
    private IntPtr _owner = owner;

    private bool IsOwnerStillValid()
        => _owner != IntPtr.Zero && XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD) == _owner;
    
    public async Task<object?> TryGetAsync(DataFormat format)
    {
        if (!IsOwnerStillValid())
            return null;

        var formatAtom = ClipboardDataFormatHelper.ToAtom(format, _textFormatAtoms, _x11.Atoms, dataFormats);
        if (formatAtom == IntPtr.Zero)
            return null;

       return await base.TryGetAsync(format, formatAtom);
    }

    public void Dispose()
        => _owner = IntPtr.Zero;
}
