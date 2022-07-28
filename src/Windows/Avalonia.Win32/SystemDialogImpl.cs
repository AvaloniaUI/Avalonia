#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;

namespace Avalonia.Win32
{
    internal class SystemDialogImpl : ISystemDialogImpl
    {
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        private const FILEOPENDIALOGOPTIONS DefaultDialogOptions = FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM | FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE |
            FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;

        public unsafe Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            var hWnd = parent?.PlatformImpl?.Handle?.Handle ?? IntPtr.Zero;
            return Task.Run(() =>
            {
                string[]? result = default;
                try
                {
                    var clsid = dialog is OpenFileDialog ? UnmanagedMethods.ShellIds.OpenFileDialog : UnmanagedMethods.ShellIds.SaveFileDialog;
                    var iid = UnmanagedMethods.ShellIds.IFileDialog;
                    var frm = UnmanagedMethods.CreateInstance<IFileDialog>(ref clsid, ref iid);

                    var openDialog = dialog as OpenFileDialog;

                    var options = frm.Options;
                    options |= DefaultDialogOptions;
                    if (openDialog?.AllowMultiple == true)
                    {
                        options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
                    }
                    frm.SetOptions(options);

                    var defaultExtension = (dialog as SaveFileDialog)?.DefaultExtension ?? "";
                    fixed (char* pExt = defaultExtension)
                    {
                        frm.SetDefaultExtension(pExt);
                    }

                    var initialFileName = dialog.InitialFileName ?? "";
                    fixed (char* fExt = initialFileName)
                    {
                        frm.SetFileName(fExt);
                    }

                    var title = dialog.Title ?? "";
                    fixed (char* tExt = title)
                    {
                        frm.SetTitle(tExt);
                    }

                    fixed (void* pFilters = FiltersToPointer(dialog.Filters, out var count))
                    {
                        frm.SetFileTypes((ushort)count, pFilters);
                    }

                    frm.SetFileTypeIndex(0);

                    if (dialog.Directory != null)
                    {
                        var riid = UnmanagedMethods.ShellIds.IShellItem;
                        if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.Directory, IntPtr.Zero, ref riid, out var directoryShellItem)
                            == (uint)UnmanagedMethods.HRESULT.S_OK)
                        {
                            var proxy = MicroComRuntime.CreateProxyFor<IShellItem>(directoryShellItem, true);
                            frm.SetFolder(proxy);
                            frm.SetDefaultFolder(proxy);
                        }
                    }

                    var showResult = frm.Show(hWnd);

                    if ((uint)showResult == (uint)UnmanagedMethods.HRESULT.E_CANCELLED)
                    {
                        return result;
                    } 
                    else if ((uint)showResult != (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        throw new Win32Exception(showResult);
                    }

                    if (openDialog?.AllowMultiple == true)
                    {
                        using var fileOpenDialog = frm.QueryInterface<IFileOpenDialog>();
                        var shellItemArray = fileOpenDialog.Results;
                        var count = shellItemArray.Count;

                        var results = new List<string>();
                        for (int i = 0; i < count; i++)
                        {
                            var shellItem = shellItemArray.GetItemAt(i);
                            if (GetAbsoluteFilePath(shellItem) is { } selected)
                            {
                                results.Add(selected);
                            }
                        }
                        result = results.ToArray();
                    }
                    else if (frm.Result is { } shellItem
                        && GetAbsoluteFilePath(shellItem) is { } singleResult)
                    {
                        result = new[] { singleResult };
                    }

                    return result;
                }
                catch (COMException ex)
                {
                    var message = new Win32Exception(ex.HResult).Message;
                    throw new COMException(message, ex);
                }
            })!;
        }

        public unsafe Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            return Task.Run(() =>
            {
                string? result = default;
                try
                {
                    var hWnd = parent?.PlatformImpl?.Handle?.Handle ?? IntPtr.Zero;
                    var clsid = UnmanagedMethods.ShellIds.OpenFileDialog;
                    var iid = UnmanagedMethods.ShellIds.IFileDialog;
                    var frm = UnmanagedMethods.CreateInstance<IFileDialog>(ref clsid, ref iid);

                    var options = frm.Options;
                    options = FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | DefaultDialogOptions;
                    frm.SetOptions(options);

                    var title = dialog.Title ?? "";
                    fixed (char* tExt = title)
                    {
                        frm.SetTitle(tExt);
                    }

                    if (dialog.Directory != null)
                    {
                        var riid = UnmanagedMethods.ShellIds.IShellItem;
                        if (UnmanagedMethods.SHCreateItemFromParsingName(dialog.Directory, IntPtr.Zero, ref riid, out var directoryShellItem)
                            == (uint)UnmanagedMethods.HRESULT.S_OK)
                        {
                            var proxy = MicroComRuntime.CreateProxyFor<IShellItem>(directoryShellItem, true);
                            frm.SetFolder(proxy);
                            frm.SetDefaultFolder(proxy);
                        }
                    }

                    var showResult = frm.Show(hWnd);

                    if ((uint)showResult == (uint)UnmanagedMethods.HRESULT.E_CANCELLED)
                    {
                        return result;
                    }
                    else if ((uint)showResult != (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        throw new Win32Exception(showResult);
                    }

                    if (frm.Result is not null)
                    {
                        result = GetAbsoluteFilePath(frm.Result);
                    }

                    return result;
                }
                catch (COMException ex)
                {
                    var message = new Win32Exception(ex.HResult).Message;
                    throw new COMException(message, ex);
                }
            });
        }

        private unsafe string? GetAbsoluteFilePath(IShellItem shellItem)
        {
            var pszString = new IntPtr(shellItem.GetDisplayName(SIGDN_FILESYSPATH));
            if (pszString != IntPtr.Zero)
            {
                try
                {
                    return Marshal.PtrToStringUni(pszString);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pszString);
                }
            }
            return default;
        }

        private unsafe byte[] FiltersToPointer(List<FileDialogFilter>? filters, out int lenght)
        {
            if (filters == null || filters.Count == 0)
            {
                filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "All files", Extensions = new List<string> { "*" } }
                };
            }

            var size = Marshal.SizeOf<UnmanagedMethods.COMDLG_FILTERSPEC>();
            var arr = new byte[size];
            var resultArr = new byte[size * filters.Count];

            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                var filterPtr = Marshal.AllocHGlobal(size);
                try
                {
                    var filterStr = new UnmanagedMethods.COMDLG_FILTERSPEC
                    {
                        pszName = filter.Name ?? string.Empty,
                        pszSpec = string.Join(";", filter.Extensions.Select(e => "*." + e))
                    };

                    Marshal.StructureToPtr(filterStr, filterPtr, false);
                    Marshal.Copy(filterPtr, resultArr, i * size, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(filterPtr);
                }
            }

            lenght = filters.Count;
            return resultArr;
        }
    }
}
