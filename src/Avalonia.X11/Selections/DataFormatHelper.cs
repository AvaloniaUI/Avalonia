using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;

namespace Avalonia.X11.Selections;

internal static class DataFormatHelper
{
    private const string AppPrefix = "application/avn-fmt.";

    public const string MimeTypeTextUriList = "text/uri-list";
    public const string MimeTypePngFormat = "image/png";
    public const string MimeTypeJpegFormat = "image/jpeg";
    public const string MimeTypeTextPlain = "text/plain";
    public const string MimeTypeTextPlainUtf8 = "text/plain;charset=utf-8";

    public static DataFormat? ToDataFormat(IntPtr formatAtom, X11Atoms atoms)
    {
        if (formatAtom == IntPtr.Zero)
            return null;

        if (atoms.TextFormats.Contains(formatAtom))
            return DataFormat.Text;

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

    public static (DataFormat[] DataFormats, IntPtr[] TextFormatAtoms) ToDataFormats(IntPtr[] formatAtoms, X11Atoms atoms)
    {
        if (formatAtoms.Length == 0)
            return ([], []);

        var formats = new List<DataFormat>(formatAtoms.Length);
        List<IntPtr>? textFormatAtoms = null;

        var hasImage = false;

        foreach (var formatAtom in formatAtoms)
        {
            if (ToDataFormat(formatAtom, atoms) is not { } format)
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
                    if (format.Identifier is MimeTypeJpegFormat or MimeTypePngFormat)
                        hasImage = true;
                }
            }
        }

        if (hasImage)
            formats.Add(DataFormat.Bitmap);

        return (formats.ToArray(), textFormatAtoms?.ToArray() ?? []);
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
                if (imageFormat.Identifier is MimeTypePngFormat)
                    pngFormat = imageFormat;
                else if (imageFormat.Identifier is MimeTypeJpegFormat)
                    jpegFormat = imageFormat;

                if (pngFormat != null && jpegFormat != null)
                    break;
            }

            var preferredFormat = pngFormat ?? jpegFormat;

            if (preferredFormat != null)
                return atoms.GetAtom(preferredFormat.Identifier);
            else
                return IntPtr.Zero;
        }

        var systemName = format.ToSystemName(AppPrefix);
        return atoms.GetAtom(systemName);
    }

    public static IntPtr[] ToAtoms(DataFormat format, X11Atoms atoms)
    {
        if (DataFormat.Text.Equals(format))
            return atoms.TextFormats;

        if (DataFormat.File.Equals(format))
            return [atoms.GetAtom(MimeTypeTextUriList)];

        if (DataFormat.Bitmap.Equals(format))
            return [atoms.GetAtom(MimeTypePngFormat)];

        var systemName = format.ToSystemName(AppPrefix);
        return [atoms.GetAtom(systemName)];
    }

    public static IntPtr[] ToAtoms(IReadOnlyList<DataFormat> formats, X11Atoms atoms)
    {
        var atomValues = new List<IntPtr>(formats.Count);

        foreach (var format in formats)
        {
            if (format.Kind == DataFormatKind.InProcess)
                continue;

            foreach (var atom in ToAtoms(format, atoms))
                atomValues.Add(atom);
        }

        return atomValues.ToArray();
    }

    private static IntPtr GetPreferredStringFormatAtom(IntPtr[] textFormatAtoms, X11Atoms atoms)
    {
        foreach (var preferredFormat in atoms.TextFormats)
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

        if (formatAtom == atoms.UTF8_STRING ||
            formatAtom == atoms.GetAtom(MimeTypeTextPlain) ||
            formatAtom == atoms.GetAtom(MimeTypeTextPlainUtf8))
        {
            return Encoding.UTF8;
        }

        if (formatAtom == atoms.STRING)
            return Encoding.ASCII;

        return null;
    }
}
