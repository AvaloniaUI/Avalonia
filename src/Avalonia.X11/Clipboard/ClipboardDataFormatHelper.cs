using System;
using System.Text;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Clipboard;

internal static class ClipboardDataFormatHelper
{
    private const string MimeTypeTextUriList = "text/uri-list";

    public static DataFormat? ToDataFormat(IntPtr formatAtom, X11Atoms atoms)
    {
        if (formatAtom == IntPtr.Zero)
            return null;

        if (formatAtom == atoms.UTF16_STRING ||
            formatAtom == atoms.UTF8_STRING ||
            formatAtom == atoms.XA_STRING ||
            formatAtom == atoms.OEMTEXT)
        {
            return DataFormat.Text;
        }

        if (formatAtom == atoms.MULTIPLE ||
            formatAtom == atoms.TARGETS ||
            formatAtom == atoms.SAVE_TARGETS)
        {
            return null;
        }

        if (atoms.GetAtomName(formatAtom) is { } atomName)
        {
            return atomName == MimeTypeTextUriList ?
                DataFormat.File :
                DataFormat.FromSystemName(atomName);
        }

        return null;
    }

    public static IntPtr ToAtom(DataFormat format, IntPtr[] textFormatAtoms, X11Atoms atoms)
    {
        if (DataFormat.Text.Equals(format))
            return GetPreferredStringFormatAtom(textFormatAtoms, atoms);

        if (DataFormat.File.Equals(format))
            return atoms.GetAtom(MimeTypeTextUriList);

        return atoms.GetAtom(format.SystemName);
    }

    public static IntPtr[] ToAtoms(DataFormat format, IntPtr[] textFormatAtoms, X11Atoms atoms)
    {
        if (DataFormat.Text.Equals(format))
            return textFormatAtoms;

        if (DataFormat.File.Equals(format))
            return [atoms.GetAtom(MimeTypeTextUriList)];

        return [atoms.GetAtom(format.SystemName)];
    }

    private static IntPtr GetPreferredStringFormatAtom(IntPtr[] textFormatAtoms, X11Atoms atoms)
    {
        ReadOnlySpan<IntPtr> preferredFormats = [atoms.UTF16_STRING, atoms.UTF8_STRING, atoms.XA_STRING];

        foreach (var preferredFormat in preferredFormats)
        {
            if (Array.IndexOf(textFormatAtoms, preferredFormat) >= 0)
                return preferredFormat;
        }

        return atoms.UTF8_STRING;
    }

    public static Encoding? TryGetStringEncoding(IntPtr formatAtom, X11Atoms atoms)
    {
        if (formatAtom == atoms.UTF16_STRING)
           return Encoding.Unicode;

        if (formatAtom == atoms.UTF8_STRING)
            return Encoding.UTF8;

        if (formatAtom == atoms.XA_STRING || formatAtom == atoms.OEMTEXT)
            return Encoding.ASCII;

        return null;
    }
}
