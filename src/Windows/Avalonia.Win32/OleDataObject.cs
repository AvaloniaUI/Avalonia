using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Avalonia.Controls.DragDrop;
using Avalonia.Win32.Interop;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Avalonia.Win32
{
    class OleDataObject : Avalonia.Controls.DragDrop.IDataObject
    {
        private IDataObject _wrapped;

        public OleDataObject(IDataObject wrapped)
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
                        return ReadBytesFromHGlobal(medium.unionmember);
                    }
                }
                finally
                {
                    UnmanagedMethods.ReleaseStgMedium(ref medium);
                }
            }
            return null;
        }

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
                FORMATETC[] formats = new FORMATETC[1];
                int[] fetched = { 1 };
                while (fetched[0] > 0)
                {
                    fetched[0] = 0;
                    if (enumFormat.Next(1, formats, fetched) == 0 && fetched[0] > 0)
                    {
                        if (formats[0].ptd != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(formats[0].ptd);
                        
                        yield return ClipboardFormats.GetFormat(formats[0].cfFormat);
                    }
                }
            }
        }
    }
}