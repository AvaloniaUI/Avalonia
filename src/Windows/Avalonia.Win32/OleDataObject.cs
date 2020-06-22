using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Avalonia.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class OleDataObject : Avalonia.Input.IDataObject
    {
        private IOleDataObject _wrapped;

        public OleDataObject(IOleDataObject wrapped)
        {
            _wrapped = wrapped;
        }

        public bool Contains(string dataFormat)
        {
            return GetDataFormatsCore().Any(df => StringComparer.OrdinalIgnoreCase.Equals(df, dataFormat));
        }

        public IEnumerable<string> GetDataFormats()
        {
            return GetDataFormatsCore().Distinct();
        }

        public string GetText()
        {
            return GetDataFromOleHGLOBAL(DataFormats.Text, DVASPECT.DVASPECT_CONTENT) as string;
        }

        public IEnumerable<string> GetFileNames()
        {
            return GetDataFromOleHGLOBAL(DataFormats.FileNames, DVASPECT.DVASPECT_CONTENT) as IEnumerable<string>;
        }

        public object Get(string dataFormat)
        {
            return GetDataFromOleHGLOBAL(dataFormat, DVASPECT.DVASPECT_CONTENT);
        }

        private object GetDataFromOleHGLOBAL(string format, DVASPECT aspect)
        {
            FORMATETC formatEtc = new FORMATETC();
            formatEtc.cfFormat = ClipboardFormats.GetFormat(format);
            formatEtc.dwAspect = aspect;
            formatEtc.lindex = -1;
            formatEtc.tymed = TYMED.TYMED_HGLOBAL;
            if (_wrapped.QueryGetData(ref formatEtc) == 0)
            {
                _wrapped.GetData(ref formatEtc, out STGMEDIUM medium);
                try
                {
                    if (medium.unionmember != IntPtr.Zero && medium.tymed == TYMED.TYMED_HGLOBAL)
                    {
                        if (format == DataFormats.Text)
                            return ReadStringFromHGlobal(medium.unionmember);
                        if (format == DataFormats.FileNames)
                            return ReadFileNamesFromHGlobal(medium.unionmember);

                        byte[] data = ReadBytesFromHGlobal(medium.unionmember);

                        if (IsSerializedObject(data))
                        {
                            using (var ms = new MemoryStream(data))
                            {
                                ms.Position = DataObject.SerializedObjectGUID.Length;
                                BinaryFormatter binaryFormatter = new BinaryFormatter();
                                return binaryFormatter.Deserialize(ms);
                            }
                        }
                        return data;
                    }
                }
                finally
                {
                    UnmanagedMethods.ReleaseStgMedium(ref medium);
                }
            }
            return null;
        }

        private static bool IsSerializedObject(ReadOnlySpan<byte> data) =>
            data.StartsWith(DataObject.SerializedObjectGUID);

        private static IEnumerable<string> ReadFileNamesFromHGlobal(IntPtr hGlobal)
        {
            List<string> files = new List<string>();
            int fileCount = UnmanagedMethods.DragQueryFile(hGlobal, -1, null, 0);
            if (fileCount > 0)
            {
                for (int i = 0; i < fileCount; i++)
                {
                    int pathLen = UnmanagedMethods.DragQueryFile(hGlobal, i, null, 0);
                    StringBuilder sb = new StringBuilder(pathLen+1);

                    if (UnmanagedMethods.DragQueryFile(hGlobal, i, sb, sb.Capacity) == pathLen)
                    {
                        files.Add(sb.ToString());
                    }
                }
            }
            return files;
        }

        private static string ReadStringFromHGlobal(IntPtr hGlobal)
        {
            IntPtr ptr = UnmanagedMethods.GlobalLock(hGlobal);
            try
            {
                return Marshal.PtrToStringAuto(ptr);
            }
            finally
            {
                UnmanagedMethods.GlobalUnlock(hGlobal);
            }
        }

        private static byte[] ReadBytesFromHGlobal(IntPtr hGlobal)
        {
            IntPtr source = UnmanagedMethods.GlobalLock(hGlobal);
            try
            {
                int size = (int)UnmanagedMethods.GlobalSize(hGlobal).ToInt64();
                byte[] data = new byte[size];
                Marshal.Copy(source, data, 0, size);
                return data;
            }
            finally
            {
                UnmanagedMethods.GlobalUnlock(hGlobal);
            }
        }

        private IEnumerable<string> GetDataFormatsCore()
        {
            var enumFormat = _wrapped.EnumFormatEtc(DATADIR.DATADIR_GET);

            if (enumFormat != null)
            {
                enumFormat.Reset();
                
                var formats = ArrayPool<FORMATETC>.Shared.Rent(1);
                var fetched = ArrayPool<int>.Shared.Rent(1);

                try
                {
                    do
                    {
                        fetched[0] = 0;
                        if (enumFormat.Next(1, formats, fetched) == 0 && fetched[0] > 0)
                        {
                            if (formats[0].ptd != IntPtr.Zero)
                                Marshal.FreeCoTaskMem(formats[0].ptd);

                            yield return ClipboardFormats.GetFormat(formats[0].cfFormat);
                        }
                    }
                    while (fetched[0] > 0);
                }
                finally
                {
                    ArrayPool<FORMATETC>.Shared.Return(formats);
                    ArrayPool<int>.Shared.Return(fetched);
                }
            }
        }
    }
}
