using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.FreeDesktop
{
    internal static class NativeMethods
    {
        [DllImport("libc", SetLastError = true)]
        private static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename,
                                            [MarshalAs(UnmanagedType.LPArray)] byte[] buffer,
                                            long len);

        public static string ReadLink(string path)
        {
            var symlink = Encoding.UTF8.GetBytes(path);
            var result = new byte[4095];
            readlink(symlink, result, result.Length);
            var rawstr = Encoding.UTF8.GetString(result);
            return rawstr.Substring(0, rawstr.IndexOf('\0'));
        }
    }
}
