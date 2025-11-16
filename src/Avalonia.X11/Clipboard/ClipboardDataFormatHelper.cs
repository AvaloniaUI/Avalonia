using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.X11.Clipboard;

internal static class ClipboardDataFormatHelper
{
    private const string MimeTypeTextUriList = "text/uri-list";
    private const string AppPrefix = "application/avn-fmt.";
    public const string PngFormatMimeType = "image/png";
    public const string JpegFormatMimeType = "image/jpeg";

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
                DataFormat.File : DataFormat.FromSystemName<byte[]>(atomName, AppPrefix);
        }

        return null;
    }

    public static IntPtr ToAtom(DataFormat format, IntPtr[] textFormatAtoms, X11Atoms atoms, DataFormat[] dataFormats)
    {
        if (DataFormat.Text.Equals(format))
            return GetPreferredStringFormatAtom(textFormatAtoms, atoms);

        if (DataFormat.File.Equals(format))
            return atoms.GetAtom(MimeTypeTextUriList);

        if (DataFormat.Bitmap.Equals(format))
        {
            DataFormat? pngFormat = null, jpegFormat = null;
            foreach (var imageFormat in dataFormats)
            {
                if (imageFormat.Identifier is PngFormatMimeType)
                    pngFormat = imageFormat;
                else if (imageFormat.Identifier is JpegFormatMimeType)
                    jpegFormat = imageFormat;

                if (pngFormat != null && jpegFormat != null)
                    break;
            }

            var preferredFormat = pngFormat ?? jpegFormat ?? null;

            if (preferredFormat != null)
                return atoms.GetAtom(preferredFormat.ToSystemName(AppPrefix));
            else
                return IntPtr.Zero;
        }

        var systemName = format.ToSystemName(AppPrefix);
        return atoms.GetAtom(systemName);
    }

    public static IntPtr[] ToAtoms(DataFormat format, IntPtr[] textFormatAtoms, X11Atoms atoms)
    {
        if (DataFormat.Text.Equals(format))
            return textFormatAtoms;

        if (DataFormat.File.Equals(format))
            return [atoms.GetAtom(MimeTypeTextUriList)];

        if (DataFormat.Bitmap.Equals(format))
            return [atoms.GetAtom(PngFormatMimeType)];

        var systemName = format.ToSystemName(AppPrefix);
        return [atoms.GetAtom(systemName)];
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
