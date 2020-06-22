using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Input;
using Avalonia.Win32.Interop;
using IDataObject = Avalonia.Input.IDataObject;

namespace Avalonia.Win32
{
    class DataObject : IDataObject, IOleDataObject
    {
        // Compatibility with WinForms + WPF...
        internal static readonly byte[] SerializedObjectGUID = new Guid("FD9EA796-3B13-4370-A679-56106BB288FB").ToByteArray();

        class FormatEnumerator : IEnumFORMATETC
        {
            private FORMATETC[] _formats;
            private int _current;

            private FormatEnumerator(FORMATETC[] formats, int current)
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

            public void Clone(out IEnumFORMATETC newEnum)
            {
                newEnum = new FormatEnumerator(_formats, _current);
            }

            public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
            {
                if (rgelt == null)
                    return unchecked((int)UnmanagedMethods.HRESULT.E_INVALIDARG);

                int i = 0;
                while (i < celt && _current < _formats.Length)
                {
                    rgelt[i] = _formats[_current];
                    _current++;
                    i++;
                }
                if (pceltFetched != null)
                    pceltFetched[0] = i;

                if (i != celt)
                    return unchecked((int)UnmanagedMethods.HRESULT.S_FALSE);
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }

            public int Reset()
            {
                _current = 0;
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }

            public int Skip(int celt)
            {
                _current += Math.Min(celt, int.MaxValue - _current);
                if (_current >= _formats.Length)
                    return unchecked((int)UnmanagedMethods.HRESULT.S_FALSE);
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            }
        }

        private const int DV_E_TYMED = unchecked((int)0x80040069);
        private const int DV_E_DVASPECT = unchecked((int)0x8004006B);
        private const int DV_E_FORMATETC = unchecked((int)0x80040064);
        private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
        private const int STG_E_MEDIUMFULL = unchecked((int)0x80030070);
        private const int GMEM_ZEROINIT = 0x0040;
        private const int GMEM_MOVEABLE = 0x0002;


        IDataObject _wrapped;
        
        public DataObject(IDataObject wrapped)
        {
            _wrapped = wrapped;
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

        IEnumerable<string> IDataObject.GetFileNames()
        {
            return _wrapped.GetFileNames();
        }

        string IDataObject.GetText()
        {
            return _wrapped.GetText();
        }

        object IDataObject.Get(string dataFormat)
        {
            return _wrapped.Get(dataFormat);
        }
        #endregion

        #region IOleDataObject

        int IOleDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            if (_wrapped is IOleDataObject ole)
                return ole.DAdvise(ref pFormatetc, advf, adviseSink, out connection);
            connection = 0;
            return OLE_E_ADVISENOTSUPPORTED;
        }

        void IOleDataObject.DUnadvise(int connection)
        {
            if (_wrapped is IOleDataObject ole)
                ole.DUnadvise(connection);
            Marshal.ThrowExceptionForHR(OLE_E_ADVISENOTSUPPORTED);
        }

        int IOleDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            if (_wrapped is IOleDataObject ole)
                return ole.EnumDAdvise(out enumAdvise);

            enumAdvise = null;
            return OLE_E_ADVISENOTSUPPORTED;
        }

        IEnumFORMATETC IOleDataObject.EnumFormatEtc(DATADIR direction)
        {
            if (_wrapped is IOleDataObject ole)
                return ole.EnumFormatEtc(direction);
            if (direction == DATADIR.DATADIR_GET)
                return new FormatEnumerator(_wrapped);
            throw new NotSupportedException();
        }

        int IOleDataObject.GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            if (_wrapped is IOleDataObject ole)
                return ole.GetCanonicalFormatEtc(ref formatIn, out formatOut);

            formatOut = new FORMATETC();
            formatOut.ptd = IntPtr.Zero;
            return unchecked((int)UnmanagedMethods.HRESULT.E_NOTIMPL);
        }

        void IOleDataObject.GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            if (_wrapped is IOleDataObject ole)
            {
                ole.GetData(ref format, out medium);
                return;
            }
            if(!format.tymed.HasFlag(TYMED.TYMED_HGLOBAL))
                Marshal.ThrowExceptionForHR(DV_E_TYMED);

            if (format.dwAspect != DVASPECT.DVASPECT_CONTENT)
                Marshal.ThrowExceptionForHR(DV_E_DVASPECT);

            string fmt = ClipboardFormats.GetFormat(format.cfFormat);
            if (string.IsNullOrEmpty(fmt) || !_wrapped.Contains(fmt))
                Marshal.ThrowExceptionForHR(DV_E_FORMATETC);

            medium = default(STGMEDIUM);
            medium.tymed = TYMED.TYMED_HGLOBAL;
            int result = WriteDataToHGlobal(fmt, ref medium.unionmember);
            Marshal.ThrowExceptionForHR(result);
        }

        void IOleDataObject.GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            if (_wrapped is IOleDataObject ole)
            {
                ole.GetDataHere(ref format, ref medium);
                return;
            }

            if (medium.tymed != TYMED.TYMED_HGLOBAL || !format.tymed.HasFlag(TYMED.TYMED_HGLOBAL))
                Marshal.ThrowExceptionForHR(DV_E_TYMED);

            if (format.dwAspect != DVASPECT.DVASPECT_CONTENT)
                Marshal.ThrowExceptionForHR(DV_E_DVASPECT);

            string fmt = ClipboardFormats.GetFormat(format.cfFormat);
            if (string.IsNullOrEmpty(fmt) || !_wrapped.Contains(fmt))
                Marshal.ThrowExceptionForHR(DV_E_FORMATETC);

            if (medium.unionmember == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(STG_E_MEDIUMFULL);

            int result = WriteDataToHGlobal(fmt, ref medium.unionmember);
            Marshal.ThrowExceptionForHR(result);
        }

        int IOleDataObject.QueryGetData(ref FORMATETC format)
        {
            if (_wrapped is IOleDataObject ole)
                return ole.QueryGetData(ref format);
            if (format.dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;
            if (!format.tymed.HasFlag(TYMED.TYMED_HGLOBAL))
                return DV_E_TYMED;

            string dataFormat = ClipboardFormats.GetFormat(format.cfFormat);
            if (!string.IsNullOrEmpty(dataFormat) && _wrapped.Contains(dataFormat))
                return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
            return DV_E_FORMATETC;
        }
        
        void IOleDataObject.SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            if (_wrapped is IOleDataObject ole)
            {
                ole.SetData(ref formatIn, ref medium, release);
                return;
            }
            Marshal.ThrowExceptionForHR(unchecked((int)UnmanagedMethods.HRESULT.E_NOTIMPL));
        }

        private int WriteDataToHGlobal(string dataFormat, ref IntPtr hGlobal)
        {
            object data = _wrapped.Get(dataFormat);
            if (dataFormat == DataFormats.Text || data is string)
                return WriteStringToHGlobal(ref hGlobal, Convert.ToString(data));
            if (dataFormat == DataFormats.FileNames && data is IEnumerable<string> files)
                return WriteFileListToHGlobal(ref hGlobal, files);
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
                var byteArr = bytes is byte[] ? (byte[])bytes : bytes.ToArray();
                return WriteBytesToHGlobal(ref hGlobal, byteArr);
            }
            return WriteBytesToHGlobal(ref hGlobal, SerializeObject(data));
        }

        private byte[] SerializeObject(object data)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(SerializedObjectGUID, 0, SerializedObjectGUID.Length);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(ms, data);
                return ms.ToArray();
            }
        }

        private unsafe int WriteBytesToHGlobal(ref IntPtr hGlobal, ReadOnlySpan<byte> data)
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

        private int WriteFileListToHGlobal(ref IntPtr hGlobal, IEnumerable<string> files)
        {
            if (!files?.Any() ?? false)
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

        private int WriteStringToHGlobal(ref IntPtr hGlobal, string data)
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

        #endregion
    }
}
