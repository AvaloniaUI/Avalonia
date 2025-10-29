﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using MicroCom.Runtime;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using IEnumFORMATETC = Avalonia.Win32.Win32Com.IEnumFORMATETC;

namespace Avalonia.Win32;

/// <summary>
/// Wraps a Win32 <see cref="Win32Com.IDataObject"/> into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="oleDataObject">The wrapped OLE data object.</param>
internal sealed class OleDataObjectToDataTransferWrapper(Win32Com.IDataObject oleDataObject)
    : PlatformDataTransfer
{
    private readonly Win32Com.IDataObject _oleDataObject = oleDataObject.CloneReference();

    protected override DataFormat[] ProvideFormats()
    {
        if (_oleDataObject.EnumFormatEtc((int)DATADIR.DATADIR_GET) is not { } enumFormat)
            return [];

        enumFormat.Reset();

        var formats = new List<DataFormat>();

        while (Next(enumFormat) is { } format)
            formats.Add(format);

        DataFormat? bmpImageFormat = null;
        DataFormat? pngImageFormat = null;
        DataFormat? jpegImageFormat = null;

        foreach (var format in formats)
        {
            if (format.Identifier == "image/png")
            {
                pngImageFormat = format;
            }

            if (format.Identifier == "image/jpg" || format.Identifier == "image/jpeg")
            {
                jpegImageFormat = format;
            }

            if (format.Identifier == "image/bmp")
            {
                bmpImageFormat = format;
            }
        }

        var hasSupportedFormat = (pngImageFormat ?? jpegImageFormat ?? bmpImageFormat ?? null) != null;

        if (hasSupportedFormat)
        {
            formats.Add(DataFormat.Image);
        }

        return formats.ToArray();

        static unsafe DataFormat? Next(IEnumFORMATETC enumFormat)
        {
            var fetched = 1u;
            FORMATETC formatEtc;

            var result = enumFormat.Next(1, &formatEtc, &fetched);
            if (result != 0 || fetched == 0)
                return null;

            if (formatEtc.ptd != IntPtr.Zero)
                Marshal.FreeCoTaskMem(formatEtc.ptd);

            return ClipboardFormatRegistry.GetFormatById(formatEtc.cfFormat);
        }
    }

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        List<DataFormat>? nonFileFormats = null;
        var items = new List<PlatformDataTransferItem>();
        var hasFiles = false;

        foreach (var format in Formats)
        {
            if (DataFormat.File.Equals(format))
            {
                if (hasFiles)
                    continue;

                // This is not ideal as we're reading the filenames ahead of time to generate the appropriate items.
                // However, it's unlikely to be a heavy operation.
                if (_oleDataObject.TryGet(format) is IEnumerable<IStorageItem> storageItems)
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
            items.Add(new OleDataObjectToDataTransferItemWrapper(_oleDataObject, Formats));

        return items.ToArray();
    }

    public override void Dispose()
        => _oleDataObject.Dispose();
}
