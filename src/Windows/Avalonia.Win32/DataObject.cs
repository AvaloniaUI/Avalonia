using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Input;
using Avalonia.MicroCom;
using Avalonia.Platform.Storage;
using Avalonia.Win32.Interop;

using FORMATETC = Avalonia.Win32.Interop.FORMATETC;
using IDataObject = Avalonia.Input.IDataObject;

namespace Avalonia.Win32
{
    internal sealed class DataObject : CallbackBase, IDataObject, Win32Com.IDataObject
    {
        // Compatibility with WinForms + WPF...
        internal static readonly byte[] SerializedObjectGUID = new Guid("FD9EA796-3B13-4370-A679-56106BB288FB").ToByteArray();

        private class FormatEnumerator : CallbackBase, Win32Com.IEnumFORMATETC
        {
            private readonly FORMATETC[] _formats;
            private uint _current;

            private FormatEnumerator(FORMATETC[] formats, uint current)
            {
                _formats = formats;
                _current = current;
            }

            public FormatEnumerator(IDataObject dataobj)
            {
                _formats = dataobj.GetDataFormats().Select(ConvertToFormatEtc).ToArray();
                _current = 0;
            }

            private FORMATETC ConvertToFormatEtc(string aFormatName)
            {
                FORMATETC result = default(FORMATETC);
                result.cfFormat = ClipboardFormats.GetFormat(aFormatName);
                result.dwAspect = DVASPECT.DVASPECT_CONTENT;
                result.ptd = IntPtr.Zero;
                result.lindex = -1;
                result.tymed = TYMED.TYMED_HGLOBAL;
                return result;
            }

            public unsafe uint Next(uint celt, FORMATETC* rgelt, uint* results)
            {
                if (rgelt == null)
                    return (uint)UnmanagedMethods.HRESULT.E_INVALIDARG;

                uint i = 0;
                while (i < celt && _current < _formats.Length)
                {
                    rgelt[i] = _formats[_current];
                    _current++;
                    i++;
                }

                if (i != celt)
                    return (uint)UnmanagedMethods.HRESULT.S_FALSE;

                // "results" parameter can be NULL if celt is 1.
                if (celt != 1 || results != default)
                    *results = i;
                return 0;
            }

            public uint Skip(uint celt)
            {
                _current += Math.Min(celt, int.MaxValue - _current);
                if (_current >= _formats.Length)
                    return (uint)UnmanagedMethods.HRESULT.S_FALSE;
                return 0;
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

        private const uint DV_E_TYMED = 0x80040069;
        private const uint DV_E_DVASPECT = 0x8004006B;
        private const uint DV_E_FORMATETC = 0x80040064;
        private const uint OLE_E_ADVISENOTSUPPORTED = 0x80040003;
        private const uint STG_E_MEDIUMFULL = 0x80030070;

        private const int GMEM_ZEROINIT = 0x0040;
        private const int GMEM_MOVEABLE = 0x0002;


        private IDataObject _wrapped;
        public IDataObject Wrapped => _wrapped;

        public DataObject(IDataObject wrapped)
        {
            _wrapped = wrapped switch
            {
                null => throw new ArgumentNullException(nameof(wrapped)),
                DataObject or OleDataObject => throw new ArgumentException($"Cannot wrap a {wrapped.GetType()}"),
                _ => wrapped
            };
        }

        #region IDataObject
        bool IDataObject.Contains(string dataFormat)
        {
            return _wrapped.Contains(dataFormat);
        }

        IEnumerable<string> IDataObject.GetDataFormats()
        {
            return _wrapped.GetDataFormats();
        }

        object? IDataObject.Get(string dataFormat)
        {
            return _wrapped.Get(dataFormat);
        }
        #endregion

        #region IOleDataObject

        unsafe int Win32Com.IDataObject.DAdvise(FORMATETC* pFormatetc, int advf, void* adviseSink)
        {
            if (_wrapped is Win32Com.IDataObject ole)
                return ole.DAdvise(pFormatetc, advf, adviseSink);
            return 0;
        }

        void Win32Com.IDataObject.DUnadvise(int connection)
        {
            if (_wrapped is Win32Com.IDataObject ole)
                ole.DUnadvise(connection);
            throw new COMException(nameof(OLE_E_ADVISENOTSUPPORTED), unchecked((int)OLE_E_ADVISENOTSUPPORTED));
        }

        unsafe void* Win32Com.IDataObject.EnumDAdvise()
        {
            if (_wrapped is Win32Com.IDataObject ole)
                return ole.EnumDAdvise();

            return null;
        }

        Win32Com.IEnumFORMATETC Win32Com.IDataObject.EnumFormatEtc(int direction)
        {
            if (_wrapped is Win32Com.IDataObject ole)
                return ole.EnumFormatEtc(direction);
            if ((DATADIR)direction == DATADIR.DATADIR_GET)
                return new FormatEnumerator(_wrapped);
            throw new COMException(nameof(UnmanagedMethods.HRESULT.E_NOTIMPL), unchecked((int)UnmanagedMethods.HRESULT.E_NOTIMPL));
        }

        unsafe FORMATETC Win32Com.IDataObject.GetCanonicalFormatEtc(FORMATETC* formatIn)
        {
            if (_wrapped is Win32Com.IDataObject ole)
                return ole.GetCanonicalFormatEtc(formatIn);

            throw new COMException(nameof(UnmanagedMethods.HRESULT.E_NOTIMPL), unchecked((int)UnmanagedMethods.HRESULT.E_NOTIMPL));
        }

        unsafe uint Win32Com.IDataObject.GetData(FORMATETC* format, Interop.STGMEDIUM* medium)
        {
            if (_wrapped is Win32Com.IDataObject ole)
            {
                return ole.GetData(format, medium);
            }

            if (!format->tymed.HasAllFlags(TYMED.TYMED_HGLOBAL))
                return DV_E_TYMED;

            if (format->dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;

            string fmt = ClipboardFormats.GetFormat(format->cfFormat);
            if (string.IsNullOrEmpty(fmt) || !_wrapped.Contains(fmt))
                return DV_E_FORMATETC;

            * medium = default;
            medium->tymed = TYMED.TYMED_HGLOBAL;
            return WriteDataToHGlobal(fmt, ref medium->unionmember);
        }

        unsafe uint Win32Com.IDataObject.GetDataHere(FORMATETC* format, Interop.STGMEDIUM* medium)
        {
            if (_wrapped is Win32Com.IDataObject ole)
            {
                return ole.GetDataHere(format, medium);
            }

            if (medium->tymed != TYMED.TYMED_HGLOBAL || !format->tymed.HasAllFlags(TYMED.TYMED_HGLOBAL))
                return DV_E_TYMED;

            if (format->dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;

            string fmt = ClipboardFormats.GetFormat(format->cfFormat);
            if (string.IsNullOrEmpty(fmt) || !_wrapped.Contains(fmt))
                return DV_E_FORMATETC;

            if (medium->unionmember == IntPtr.Zero)
                return STG_E_MEDIUMFULL;

            return WriteDataToHGlobal(fmt, ref medium->unionmember);
        }

        unsafe uint Win32Com.IDataObject.QueryGetData(FORMATETC* format)
        {
            if (_wrapped is Win32Com.IDataObject ole)
            {
                return ole.QueryGetData(format);
            }

            if (format->dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;
            if (!format->tymed.HasAllFlags(TYMED.TYMED_HGLOBAL))
                return DV_E_TYMED;

            var dataFormat = ClipboardFormats.GetFormat(format->cfFormat);

            if (string.IsNullOrEmpty(dataFormat) || !_wrapped.Contains(dataFormat))
                return DV_E_FORMATETC;

            return 0;
        }
        
        unsafe uint Win32Com.IDataObject.SetData(FORMATETC* pformatetc, Interop.STGMEDIUM* pmedium, int fRelease)
        {
            if (_wrapped is Win32Com.IDataObject ole)
            {
                return ole.SetData(pformatetc, pmedium, fRelease);
            }
            return (uint)UnmanagedMethods.HRESULT.E_NOTIMPL;
        }

        private uint WriteDataToHGlobal(string dataFormat, ref IntPtr hGlobal)
        {
            object data = _wrapped.Get(dataFormat)!;
            if (dataFormat == DataFormats.Text || data is string)
                return WriteStringToHGlobal(ref hGlobal, Convert.ToString(data) ?? string.Empty);
#pragma warning disable CS0618 // Type or member is obsolete
            if (dataFormat == DataFormats.FileNames && data is IEnumerable<string> files)
                return WriteFileListToHGlobal(ref hGlobal, files);
#pragma warning restore CS0618 // Type or member is obsolete
            if (dataFormat == DataFormats.Files && data is IEnumerable<IStorageItem> items)
                return WriteFileListToHGlobal(ref hGlobal, items.Select(f => f.TryGetLocalPath()).Where(f => f is not null)!);
            if (data is Stream stream)
            {
                var length = (int)(stream.Length - stream.Position);
                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    stream.Read(buffer, 0, length);
                    return WriteBytesToHGlobal(ref hGlobal, buffer.AsSpan(0, length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            if (data is IEnumerable<byte> bytes)
            {
                var byteArr = bytes as byte[] ?? bytes.ToArray();
                return WriteBytesToHGlobal(ref hGlobal, byteArr);
            }
            return WriteBytesToHGlobal(ref hGlobal, SerializeObject(data));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms dragndrop compatability")]
        private static byte[] SerializeObject(object data)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(SerializedObjectGUID, 0, SerializedObjectGUID.Length);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                binaryFormatter.Serialize(ms, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                return ms.ToArray();
            }
        }

        private static unsafe uint WriteBytesToHGlobal(ref IntPtr hGlobal, ReadOnlySpan<byte> data)
        {
            int required = data.Length;
            if (hGlobal == IntPtr.Zero)
                hGlobal = UnmanagedMethods.GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, required);

            long available = UnmanagedMethods.GlobalSize(hGlobal).ToInt64();
            if (required > available)
                return STG_E_MEDIUMFULL;

            var ptr = UnmanagedMethods.GlobalLock(hGlobal);
            Debug.Assert(ptr == hGlobal);

            try
            {
                data.CopyTo(new Span<byte>((void*)ptr, data.Length));
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }
            finally
            {
                UnmanagedMethods.GlobalUnlock(hGlobal);
            }
        }

        private static uint WriteFileListToHGlobal(ref IntPtr hGlobal, IEnumerable<string> files)
        {
            if (!files.Any())
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);

            char[] filesStr = (string.Join("\0", files) + "\0\0").ToCharArray();
            _DROPFILES df = new _DROPFILES();
            df.pFiles = Marshal.SizeOf<_DROPFILES>();
            df.fWide = true;
            
            int required = (filesStr.Length * sizeof(char)) + Marshal.SizeOf<_DROPFILES>();
            if (hGlobal == IntPtr.Zero)
                hGlobal = UnmanagedMethods.GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, required);

            long available = UnmanagedMethods.GlobalSize(hGlobal).ToInt64();
            if (required > available)
                return STG_E_MEDIUMFULL;

            IntPtr ptr = UnmanagedMethods.GlobalLock(hGlobal);
            try
            {
                Marshal.StructureToPtr(df, ptr, false);

                Marshal.Copy(filesStr, 0, ptr + Marshal.SizeOf<_DROPFILES>(), filesStr.Length);
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }
            finally
            {
                UnmanagedMethods.GlobalUnlock(hGlobal);
            }
        }

        private static uint WriteStringToHGlobal(ref IntPtr hGlobal, string data)
        {
            int required = (data.Length + 1) * sizeof(char);
            if (hGlobal == IntPtr.Zero)
                hGlobal = UnmanagedMethods.GlobalAlloc(GMEM_MOVEABLE|GMEM_ZEROINIT, required);

            long available = UnmanagedMethods.GlobalSize(hGlobal).ToInt64();
            if (required > available)
                return STG_E_MEDIUMFULL;
            
            IntPtr ptr = UnmanagedMethods.GlobalLock(hGlobal);
            try
            {
                char[] chars = (data + '\0').ToCharArray();
                Marshal.Copy(chars, 0, ptr, chars.Length);
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }
            finally
            {
                UnmanagedMethods.GlobalUnlock(hGlobal);
            }
        }

        protected override void Destroyed()
        {
            ReleaseWrapped();
        }

        public void ReleaseWrapped()
        {
            _wrapped = null!;
        }
        #endregion
    }
}
