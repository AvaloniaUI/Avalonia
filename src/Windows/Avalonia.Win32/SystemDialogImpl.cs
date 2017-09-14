using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{

    class SystemDialogImpl : ISystemDialogImpl
    {
        static char[] ToChars(string s)
        {
            if (s == null)
                return null;
            var chars = new char[s.Length];
            for (int c = 0; c < s.Length; c++)
                chars[c] = s[c];
            return chars;
        }

        public unsafe Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var hWnd = parent?.Handle?.Handle ?? IntPtr.Zero;
            return Task.Factory.StartNew(() =>
            {
                var filters = new StringBuilder();
                foreach (var filter in dialog.Filters)
                {
                    var extMask = string.Join(";", filter.Extensions.Select(e => "*." + e));
                    filters.Append(filter.Name);
                    filters.Append(" (");
                    filters.Append(extMask);
                    filters.Append(")");
                    filters.Append('\0');
                    filters.Append(extMask);
                    filters.Append('\0');
                }
                if (filters.Length == 0)
                    filters.Append("All files\0*.*\0");
                filters.Append('\0');

                var filterBuffer = new char[filters.Length];
                filters.CopyTo(0, filterBuffer, 0, filterBuffer.Length);

                var defExt = ToChars((dialog as SaveFileDialog)?.DefaultExtension);
                var fileBuffer = new char[256];
                dialog.InitialFileName?.CopyTo(0, fileBuffer, 0, dialog.InitialFileName.Length);

                string userSelectedExt = null;


                var title = ToChars(dialog.Title);
                var initialDir = ToChars(dialog.InitialDirectory);

                fixed (char* pFileBuffer = fileBuffer)
                fixed (char* pFilterBuffer = filterBuffer)
                fixed (char* pDefExt = defExt)
                fixed (char* pInitDir = initialDir)
                fixed (char* pTitle = title)
                {
                    var ofn = new UnmanagedMethods.OpenFileName()
                    {
                        hwndOwner = hWnd,
                        hInstance = IntPtr.Zero,
                        lCustData = IntPtr.Zero,
                        nFilterIndex = 0,
                        Flags =
                            UnmanagedMethods.OpenFileNameFlags.OFN_EXPLORER |
                            UnmanagedMethods.OpenFileNameFlags.OFN_HIDEREADONLY,
                        nMaxCustFilter = 0,
                        nMaxFile = fileBuffer.Length - 1,
                        nMaxFileTitle = 0,
                        lpTemplateName = IntPtr.Zero,
                        lpfnHook = IntPtr.Zero,
                        lpstrCustomFilter = IntPtr.Zero,
                        lpstrDefExt = new IntPtr(pDefExt),
                        lpstrFile = new IntPtr(pFileBuffer),
                        lpstrFileTitle = IntPtr.Zero,
                        lpstrFilter = new IntPtr(pFilterBuffer),
                        lpstrInitialDir = new IntPtr(pInitDir),
                        lpstrTitle = new IntPtr(pTitle),

                    };
                    ofn.lStructSize = Marshal.SizeOf(ofn);
                    if ((dialog as OpenFileDialog)?.AllowMultiple == true)
                        ofn.Flags |= UnmanagedMethods.OpenFileNameFlags.OFN_ALLOWMULTISELECT;

                    if (dialog is SaveFileDialog)
                        ofn.Flags |= UnmanagedMethods.OpenFileNameFlags.OFN_NOREADONLYRETURN |
                                     UnmanagedMethods.OpenFileNameFlags.OFN_OVERWRITEPROMPT;

                    var pofn = &ofn;

                    // We should save the current directory to restore it later.
                    var currentDirectory = Environment.CurrentDirectory;

                    var res = dialog is OpenFileDialog
                        ? UnmanagedMethods.GetOpenFileName(new IntPtr(pofn))
                        : UnmanagedMethods.GetSaveFileName(new IntPtr(pofn));

                    // Restore the old current directory, since GetOpenFileName and GetSaveFileName change it after they're called
                    Environment.CurrentDirectory = currentDirectory;

                    if (!res)
                        return null;
                    if (dialog?.Filters.Count > 0)
                        userSelectedExt = dialog.Filters[ofn.nFilterIndex - 1].Extensions.FirstOrDefault();
                }
                var cStart = 0;
                string dir = null;
                var files = new List<string>();
                for (var c = 0; c < fileBuffer.Length; c++)
                {
                    if (fileBuffer[c] == 0)
                    {
                        //Encountered double zero char
                        if (cStart == c)
                            break;

                        var s = new string(fileBuffer, cStart, c - cStart);
                        if (dir == null)
                            dir = s;
                        else
                            files.Add(s);
                        cStart = c + 1;
                    }
                }
                if (files.Count == 0)
                {
                    if (dialog is SaveFileDialog)
                    {
                        if (string.IsNullOrWhiteSpace(Path.GetExtension(dir)) &&
                            !string.IsNullOrWhiteSpace(userSelectedExt) &&
                            !userSelectedExt.Contains("*"))
                            dir = Path.ChangeExtension(dir, userSelectedExt);
                    }

                    return new[] { dir };
                }

                return files.Select(f => Path.Combine(dir, f)).ToArray();
            });
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            return Task.Factory.StartNew(() =>
            {
                string result = string.Empty;

                var hWnd = parent?.Handle?.Handle ?? IntPtr.Zero;
                var clsid = Guid.Parse("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
                var iid  = Guid.Parse("42F85136-DB7E-439C-85F1-E4075D135FC8");

                UnmanagedMethods.CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var unk);
                var frm = (IFileDialog)unk;
                uint options;
                frm.GetOptions(out options);
                options |= (uint)(UnmanagedMethods.FOS.FOS_PICKFOLDERS | UnmanagedMethods.FOS.FOS_FORCEFILESYSTEM | UnmanagedMethods.FOS.FOS_NOVALIDATE | UnmanagedMethods.FOS.FOS_NOTESTFILECREATE | UnmanagedMethods.FOS.FOS_DONTADDTORECENT);
                frm.SetOptions(options);

                if (dialog.InitialDirectory != null)
                {
                    IShellItem directoryShellItem;
                    var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                    if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.InitialDirectory, IntPtr.Zero, ref riid, out directoryShellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        frm.SetFolder(directoryShellItem);
                    }
                }

                if (dialog.DefaultDirectory != null)
                {
                    IShellItem directoryShellItem;
                    var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                    if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.DefaultDirectory, IntPtr.Zero, ref riid, out directoryShellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        frm.SetDefaultFolder(directoryShellItem);
                    }
                }

                if (frm.Show(hWnd) == (uint)UnmanagedMethods.HRESULT.S_OK)
                {
                    IShellItem shellItem;
                    if (frm.GetResult(out shellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        IntPtr pszString;
                        if (shellItem.GetDisplayName(UnmanagedMethods.SIGDN_FILESYSPATH, out pszString) == (uint)UnmanagedMethods.HRESULT.S_OK)
                        {
                            if (pszString != IntPtr.Zero)
                            {
                                try
                                {
                                    result = Marshal.PtrToStringAuto(pszString);
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pszString);
                                }
                            }
                        }
                    }
                }

                return result;
            });
        }
    }
}
