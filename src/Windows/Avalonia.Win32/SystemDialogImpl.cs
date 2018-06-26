using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Avalonia.Win32
{

    class SystemDialogImpl : ISystemDialogImpl
    {
        public unsafe Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var hWnd = parent?.Handle?.Handle ?? IntPtr.Zero;
            return Task.Factory.StartNew(() =>
            {
                var result = new string[0];

                Guid clsid = dialog is OpenFileDialog ? UnmanagedMethods.ShellIds.OpenFileDialog : UnmanagedMethods.ShellIds.SaveFileDialog;
                Guid iid = UnmanagedMethods.ShellIds.IFileDialog;
                UnmanagedMethods.CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var unk);
                var frm = (UnmanagedMethods.IFileDialog)unk;

                var openDialog = dialog as OpenFileDialog;

                uint options;
                frm.GetOptions(out options);
                options |= (uint)(UnmanagedMethods.FOS.FOS_NOVALIDATE | UnmanagedMethods.FOS.FOS_NOTESTFILECREATE | UnmanagedMethods.FOS.FOS_DONTADDTORECENT);
                if (openDialog?.AllowMultiple == true)
                    options |= (uint)UnmanagedMethods.FOS.FOS_ALLOWMULTISELECT;
                frm.SetOptions(options);

                var defaultExtension = (dialog as SaveFileDialog)?.DefaultExtension ?? "";
                frm.SetDefaultExtension(defaultExtension);
                frm.SetFileName(dialog.InitialFileName ?? "");
                frm.SetTitle(dialog.Title);

                var filters = new List<UnmanagedMethods.COMDLG_FILTERSPEC>();
                foreach (var filter in dialog.Filters)
                {
                    var extMask = string.Join(";", filter.Extensions.Select(e => "*." + e));
                    filters.Add(new UnmanagedMethods.COMDLG_FILTERSPEC { pszName = filter.Name, pszSpec = extMask });
                }
                if (filters.Count == 0)
                    filters.Add(new UnmanagedMethods.COMDLG_FILTERSPEC { pszName = "All files", pszSpec = "*.*" });

                frm.SetFileTypes((uint)filters.Count, filters.ToArray());
                frm.SetFileTypeIndex(0);

                if (dialog.InitialDirectory != null)
                {
                    UnmanagedMethods.IShellItem directoryShellItem;
                    Guid riid = UnmanagedMethods.ShellIds.IShellItem;
                    if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.InitialDirectory, IntPtr.Zero, ref riid, out directoryShellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        frm.SetFolder(directoryShellItem);
                        frm.SetDefaultFolder(directoryShellItem);
                    }
                }

                if (frm.Show(hWnd) == (uint)UnmanagedMethods.HRESULT.S_OK)
                {
                    if (openDialog?.AllowMultiple == true)
                    {
                        UnmanagedMethods.IShellItemArray shellItemArray;
                        ((UnmanagedMethods.IFileOpenDialog)frm).GetResults(out shellItemArray);
                        uint count;
                        shellItemArray.GetCount(out count);
                        result = new string[count];
                        for (uint i = 0; i < count; i++)
                        {
                            UnmanagedMethods.IShellItem shellItem;
                            shellItemArray.GetItemAt(i, out shellItem);
                            result[i] = GetAbsoluteFilePath(shellItem);
                        }
                    }
                    else
                    {
                        UnmanagedMethods.IShellItem shellItem;
                        if (frm.GetResult(out shellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                        {
                            result = new string[] { GetAbsoluteFilePath(shellItem) };
                        }
                    }
                }

                return result;
            });
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            return Task.Factory.StartNew(() =>
            {
                string result = string.Empty;

                var hWnd = parent?.Handle?.Handle ?? IntPtr.Zero;
                Guid clsid = UnmanagedMethods.ShellIds.OpenFileDialog;
                Guid iid  = UnmanagedMethods.ShellIds.IFileDialog;

                UnmanagedMethods.CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var unk);
                var frm = (UnmanagedMethods.IFileDialog)unk;
                uint options;
                frm.GetOptions(out options);
                options |= (uint)(UnmanagedMethods.FOS.FOS_PICKFOLDERS | UnmanagedMethods.FOS.FOS_FORCEFILESYSTEM | UnmanagedMethods.FOS.FOS_NOVALIDATE | UnmanagedMethods.FOS.FOS_NOTESTFILECREATE | UnmanagedMethods.FOS.FOS_DONTADDTORECENT);
                frm.SetOptions(options);

                if (dialog.InitialDirectory != null)
                {
                    UnmanagedMethods.IShellItem directoryShellItem;
                    Guid riid = UnmanagedMethods.ShellIds.IShellItem;
                    if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.InitialDirectory, IntPtr.Zero, ref riid, out directoryShellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        frm.SetFolder(directoryShellItem);
                    }
                }

                if (dialog.DefaultDirectory != null)
                {
                    UnmanagedMethods.IShellItem directoryShellItem;
                    Guid riid = UnmanagedMethods.ShellIds.IShellItem;
                    if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.DefaultDirectory, IntPtr.Zero, ref riid, out directoryShellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        frm.SetDefaultFolder(directoryShellItem);
                    }
                }

                if (frm.Show(hWnd) == (uint)UnmanagedMethods.HRESULT.S_OK)
                {
                    UnmanagedMethods.IShellItem shellItem;
                    if (frm.GetResult(out shellItem) == (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        result = GetAbsoluteFilePath(shellItem);
                    }
                }

                return result;
            });
        }

        private string GetAbsoluteFilePath(UnmanagedMethods.IShellItem shellItem)
        {
            IntPtr pszString;
            if (shellItem.GetDisplayName(UnmanagedMethods.SIGDN_FILESYSPATH, out pszString) == (uint)UnmanagedMethods.HRESULT.S_OK)
            {
                if (pszString != IntPtr.Zero)
                {
                    try
                    {
                        return Marshal.PtrToStringAuto(pszString);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pszString);
                    }
                }
            }
            return "";
        }
    }
}
