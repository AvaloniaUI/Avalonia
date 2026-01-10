using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.MicroCom;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;

namespace Avalonia.Win32;

/// <summary>
/// Wraps an Avalonia <see cref="IDataTransfer"/> into a Win32 <see cref="Win32Com.IDataObject"/>.
/// </summary>
/// <param name="dataTransfer">The wrapped data transfer instance.</param>
internal class DataTransferToOleDataObjectWrapper(IDataTransfer dataTransfer)
    : CallbackBase, Win32Com.IDataObject
{
    private class FormatEnumerator : CallbackBase, Win32Com.IEnumFORMATETC
    {
        private readonly FORMATETC[] _formats;
        private uint _current;

        private FormatEnumerator(FORMATETC[] formats, uint current)
        {
            _formats = formats;
            _current = current;
        }

        public FormatEnumerator(IReadOnlyList<DataFormat> dataFormats)
        {
            List<FORMATETC> formats = new List<FORMATETC>();

            if (dataFormats.Contains(DataFormat.Bitmap))
            {
                // We add extra formats for bitmaps
                formats.Add(OleDataObjectHelper.ToFormatEtc(ClipboardFormatRegistry.PngMimeDataFormat));
                formats.Add(OleDataObjectHelper.ToFormatEtc(ClipboardFormatRegistry.PngSystemDataFormat));
                formats.Add(OleDataObjectHelper.ToFormatEtc(ClipboardFormatRegistry.DibDataFormat));
                formats.Add(OleDataObjectHelper.ToFormatEtc(ClipboardFormatRegistry.DibV5DataFormat));
                formats.Add(OleDataObjectHelper.ToFormatEtc(ClipboardFormatRegistry.HBitmapDataFormat, true));
            }
            else
            {
                formats.AddRange(dataFormats.Select(x => OleDataObjectHelper.ToFormatEtc(x)));
            }

            _formats = formats.ToArray();
            _current = 0;
        }

        public unsafe uint Next(uint celt, FORMATETC* rgelt, uint* results)
        {
            if (rgelt == null)
                return (uint)HRESULT.E_INVALIDARG;

            uint i = 0;
            while (i < celt && _current < _formats.Length)
            {
                rgelt[i] = _formats[_current];
                _current++;
                i++;
            }

            if (i != celt)
                return (uint)HRESULT.S_FALSE;

            // "results" parameter can be NULL if celt is 1.
            if (celt != 1 || results != null)
                *results = i;
            return (uint)HRESULT.S_OK;
        }

        public uint Skip(uint celt)
        {
            _current += Math.Min(celt, int.MaxValue - _current);
            if (_current >= _formats.Length)
                return (uint)HRESULT.S_FALSE;
            return (uint)HRESULT.S_OK;
        }

        public void Reset()
        {
            _current = 0;
        }

        public Win32Com.IEnumFORMATETC Clone()
        {
            return new FormatEnumerator(_formats, _current);
        }
    }

    public IDataTransfer? DataTransfer { get; private set; } = dataTransfer;

    public bool IsDisposed
        => DataTransfer is null;

    public event Action? OnDestroyed;

    unsafe int Win32Com.IDataObject.DAdvise(FORMATETC* pFormatetc, int advf, void* adviseSink)
        => (int)HRESULT.S_OK;

    void Win32Com.IDataObject.DUnadvise(int connection)
        => throw new COMException(nameof(OLE_E_ADVISENOTSUPPORTED), unchecked((int)OLE_E_ADVISENOTSUPPORTED));

    unsafe void* Win32Com.IDataObject.EnumDAdvise()
        => null;

    Win32Com.IEnumFORMATETC Win32Com.IDataObject.EnumFormatEtc(int direction)
    {
        if (DataTransfer is null)
            throw new COMException(nameof(COR_E_OBJECTDISPOSED), unchecked((int)HRESULT.E_NOTIMPL));

        if ((DATADIR)direction == DATADIR.DATADIR_GET)
            return new FormatEnumerator(DataTransfer.Formats);

        throw new COMException(nameof(HRESULT.E_NOTIMPL), unchecked((int)HRESULT.E_NOTIMPL));
    }

    unsafe FORMATETC Win32Com.IDataObject.GetCanonicalFormatEtc(FORMATETC* formatIn)
        => throw new COMException(nameof(HRESULT.E_NOTIMPL), unchecked((int)HRESULT.E_NOTIMPL));

    unsafe uint Win32Com.IDataObject.GetData(FORMATETC* format, STGMEDIUM* medium)
    {
        if (!ValidateFormat(format, out var result, out var dataFormat))
            return result;

        if (format->tymed == TYMED.TYMED_GDI)
        {
            *medium = default;
            medium->tymed = TYMED.TYMED_GDI;
            return OleDataObjectHelper.WriteDataToGdi(DataTransfer, dataFormat, ref medium->unionmember);
        }
        else
        {
            *medium = default;
            medium->tymed = TYMED.TYMED_HGLOBAL;
            return OleDataObjectHelper.WriteDataToHGlobal(DataTransfer, dataFormat, ref medium->unionmember);
        }
    }

    unsafe uint Win32Com.IDataObject.GetDataHere(FORMATETC* format, STGMEDIUM* medium)
    {
        if (!ValidateFormat(format, out var result, out var dataFormat))
            return result;

        if (medium->unionmember == IntPtr.Zero)
            return STG_E_MEDIUMFULL;

        return OleDataObjectHelper.WriteDataToHGlobal(DataTransfer, dataFormat, ref medium->unionmember);
    }

    unsafe uint Win32Com.IDataObject.QueryGetData(FORMATETC* format)
    {
        if (!ValidateFormat(format, out var result, out _))
            return result;

        return (uint)HRESULT.S_OK;
    }

    [MemberNotNullWhen(true, nameof(DataTransfer))]
    private unsafe bool ValidateFormat(FORMATETC* format, out uint result, [NotNullWhen(true)] out DataFormat? dataFormat)
    {
        dataFormat = null;

        if (!(format->tymed.HasAllFlags(TYMED.TYMED_HGLOBAL) || format->cfFormat == (ushort)ClipboardFormat.CF_BITMAP && format->tymed == TYMED.TYMED_GDI))
        {
            result = DV_E_TYMED;
            dataFormat = null;
            return false;
        }

        if (format->dwAspect != DVASPECT.DVASPECT_CONTENT)
        {
            result = DV_E_DVASPECT;
            return false;
        }

        if (DataTransfer is null)
        {
            result = COR_E_OBJECTDISPOSED;
            return false;
        }

        dataFormat = ClipboardFormatRegistry.GetFormatById(format->cfFormat);
        if (!DataTransfer.Contains(dataFormat) && !ClipboardFormatRegistry.ImageFormats.Contains(dataFormat))
        {
            result = DV_E_FORMATETC;
            return false;
        }

        result = (uint)HRESULT.S_OK;
        return true;
    }

    unsafe uint Win32Com.IDataObject.SetData(FORMATETC* pformatetc, STGMEDIUM* pmedium, int fRelease)
        => (uint)HRESULT.E_NOTIMPL;

    protected override void Destroyed()
    {
        OnDestroyed?.Invoke();
        ReleaseDataTransfer();
    }

    public void ReleaseDataTransfer()
    {
        DataTransfer?.Dispose();
        DataTransfer = null;
    }
}
