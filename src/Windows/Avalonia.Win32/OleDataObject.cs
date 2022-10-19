using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Input;
using Avalonia.MicroCom;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;
using IDataObject = Avalonia.Input.IDataObject;

namespace Avalonia.Win32
{
    internal class OleDataObject : IDataObject, IDisposable
    {
        private readonly Win32Com.IDataObject _wrapped;

        public OleDataObject(Win32Com.IDataObject wrapped)
        {
            _wrapped = wrapped.CloneReference();
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
            return (string)GetDataFromOleHGLOBAL(DataFormats.Text, DVASPECT.DVASPECT_CONTENT);
        }

        public IEnumerable<string> GetFileNames()
        {
            return (IEnumerable<string>)GetDataFromOleHGLOBAL(DataFormats.FileNames, DVASPECT.DVASPECT_CONTENT);
        }

        public object Get(string dataFormat)
        {
            return GetDataFromOleHGLOBAL(dataFormat, DVASPECT.DVASPECT_CONTENT);
        }

        private unsafe object GetDataFromOleHGLOBAL(string format, DVASPECT aspect)
        {
            var formatEtc = new Interop.FORMATETC();
            formatEtc.cfFormat = ClipboardFormats.GetFormat(format);
            formatEtc.dwAspect = aspect;
            formatEtc.lindex = -1;
            formatEtc.tymed = TYMED.TYMED_HGLOBAL;
            if (_wrapped.QueryGetData(&formatEtc) == 0)
            {
                Interop.STGMEDIUM medium = default;
                _ = _wrapped.GetData(&formatEtc, &medium);
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
                    var sb = StringBuilderCache.Acquire(pathLen+1);

                    if (UnmanagedMethods.DragQueryFile(hGlobal, i, sb, sb.Capacity) == pathLen)
                    {
                        files.Add(StringBuilderCache.GetStringAndRelease(sb));
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

        private unsafe IEnumerable<string> GetDataFormatsCore()
        {
            var formatsList = new List<string>();
            var enumFormat = _wrapped.EnumFormatEtc((int)DATADIR.DATADIR_GET);

            if (enumFormat != null)
            {
                enumFormat.Reset();
                
                var formats = ArrayPool<Interop.FORMATETC>.Shared.Rent(1);

                try
                {
                    uint fetched = 0;
                    do
                    {
                        fixed (Interop.FORMATETC* formatsPtr = formats)
                        {
                            // Read one item at a time.
                            // When "celt" parameter is 1, "pceltFetched" is ignored.
                            var res = enumFormat.Next(1, formatsPtr, &fetched);
                            if (res != 0)
                            {
                                break;
                            }
                        }
                        if (fetched > 0)
                        {
                            if (formats[0].ptd != IntPtr.Zero)
                                Marshal.FreeCoTaskMem(formats[0].ptd);

                            formatsList.Add(ClipboardFormats.GetFormat(formats[0].cfFormat));
                        }
                    }
                    while (fetched > 0);
                }
                finally
                {
                    ArrayPool<Interop.FORMATETC>.Shared.Return(formats);
                }
            }
            return formatsList;
        }

        public void Dispose()
        {
            _wrapped.Dispose();
        }
    }
}
