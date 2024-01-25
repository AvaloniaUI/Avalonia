using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal class Win32StorageProvider : BclStorageProvider
    {
        private const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

        private const FILEOPENDIALOGOPTIONS DefaultDialogOptions =
            FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
            FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE | FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
            FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;

        private readonly WindowImpl _windowImpl;

        public Win32StorageProvider(WindowImpl windowImpl)
        {
            _windowImpl = windowImpl;
        }

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => true;

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            return await ShowFilePicker(
                true, true,
                options.AllowMultiple, false,
                options.Title, options.SuggestedFileName, options.SuggestedStartLocation, null, null,
                f => new BclStorageFolder(new DirectoryInfo(f)))
                .ConfigureAwait(false);
        }

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            return await ShowFilePicker(
                true, false,
                options.AllowMultiple, false,
                options.Title, options.SuggestedFileName, options.SuggestedStartLocation,
                null, options.FileTypeFilter,
                f => new BclStorageFile(new FileInfo(f)))
                .ConfigureAwait(false);
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var files = await ShowFilePicker(
                false, false,
                false, options.ShowOverwritePrompt,
                options.Title, options.SuggestedFileName, options.SuggestedStartLocation,
                options.DefaultExtension, options.FileTypeChoices,
                f => new BclStorageFile(new FileInfo(f)))
                .ConfigureAwait(false);
            return files.Count > 0 ? files[0] : null;
        }

        private unsafe Task<IReadOnlyList<TStorageItem>> ShowFilePicker<TStorageItem>(
            bool isOpenFile,
            bool openFolder,
            bool allowMultiple,
            bool? showOverwritePrompt,
            string? title,
            string? suggestedFileName,
            IStorageFolder? folder,
            string? defaultExtension,
            IReadOnlyList<FilePickerFileType>? filters,
            Func<string, TStorageItem> convert)
            where TStorageItem : IStorageItem
        {
            return Task.Run(() =>
            {
                IReadOnlyList<TStorageItem> result = Array.Empty<TStorageItem>();
                try
                {
                    var clsid = isOpenFile ? UnmanagedMethods.ShellIds.OpenFileDialog : UnmanagedMethods.ShellIds.SaveFileDialog;
                    var iid = UnmanagedMethods.ShellIds.IFileDialog;
                    var frm = UnmanagedMethods.CreateInstance<IFileDialog>(in clsid, in iid);

                    var options = frm.Options;
                    options |= DefaultDialogOptions;
                    if (openFolder)
                    {
                        options |= FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS;
                    }
                    if (allowMultiple)
                    {
                        options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
                    }

                    if (showOverwritePrompt == false)
                    {
                        options &= ~FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT;
                    }
                    frm.SetOptions(options);

                    if (defaultExtension is null)
                    {
                        defaultExtension = string.Empty;
                    }

                    fixed (char* pExt = defaultExtension)
                    {
                        frm.SetDefaultExtension(pExt);
                    }

                    suggestedFileName ??= "";
                    fixed (char* fExt = suggestedFileName)
                    {
                        frm.SetFileName(fExt);
                    }

                    title ??= "";
                    fixed (char* tExt = title)
                    {
                        frm.SetTitle(tExt);
                    }

                    if (!openFolder)
                    {
                        fixed (void* pFilters = FiltersToPointer(filters, out var count))
                        {
                            frm.SetFileTypes((ushort)count, pFilters);
                            if (count > 0)
                            {
                                frm.SetFileTypeIndex(0);
                            }
                        }
                    }

                    if (folder?.TryGetLocalPath() is { } folderPath)
                    {
                        var riid = UnmanagedMethods.ShellIds.IShellItem;
                        if (UnmanagedMethods.SHCreateItemFromParsingName(folderPath, IntPtr.Zero, ref riid, out var directoryShellItem)
                            == (uint)UnmanagedMethods.HRESULT.S_OK)
                        {
                            var proxy = MicroComRuntime.CreateProxyFor<IShellItem>(directoryShellItem, true);
                            frm.SetFolder(proxy);
                            frm.SetDefaultFolder(proxy);
                        }
                    }

                    var showResult = frm.Show(_windowImpl.Handle.Handle);

                    if ((uint)showResult == (uint)UnmanagedMethods.HRESULT.E_CANCELLED)
                    {
                        return result;
                    }
                    else if ((uint)showResult != (uint)UnmanagedMethods.HRESULT.S_OK)
                    {
                        throw new Win32Exception(showResult);
                    }

                    if (allowMultiple)
                    {
                        using var fileOpenDialog = frm.QueryInterface<IFileOpenDialog>();
                        var shellItemArray = fileOpenDialog.Results;
                        var count = shellItemArray.Count;

                        var results = new List<TStorageItem>();
                        for (int i = 0; i < count; i++)
                        {
                            var shellItem = shellItemArray.GetItemAt(i);
                            if (GetParsingName(shellItem) is { } selected)
                            {
                                results.Add(convert(selected));
                            }
                        }

                        result = results;
                    }
                    else if (frm.Result is { } shellItem
                        && GetParsingName(shellItem) is { } singleResult)
                    {
                        result = new[] { convert(singleResult) };
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


        private static string? GetParsingName(IShellItem shellItem)
        {
            return GetDisplayName(shellItem, SIGDN_DESKTOPABSOLUTEPARSING);
        }
        
        private static unsafe string? GetDisplayName(IShellItem shellItem, uint sigdnName)
        {
            char* pszString = null;
            if (shellItem.GetDisplayName(sigdnName, &pszString) == 0)
            {
                try
                {
                    return Marshal.PtrToStringUni((IntPtr)pszString);
                }
                finally
                {
                    Marshal.FreeCoTaskMem((IntPtr)pszString);
                }
            }
            return default;
        }

        private static byte[] FiltersToPointer(IReadOnlyList<FilePickerFileType>? filters, out int length)
        {
            if (filters == null || filters.Count == 0)
            {
                filters = new List<FilePickerFileType>
                {
                    FilePickerFileTypes.All
                };
            }

            var size = Marshal.SizeOf<UnmanagedMethods.COMDLG_FILTERSPEC>();
            var resultArr = new byte[size * filters.Count];

            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                if (filter.Patterns is null || filter.Patterns.Count == 0)
                {
                    continue;
                }

                var filterPtr = Marshal.AllocHGlobal(size);
                try
                {
                    var filterStr = new UnmanagedMethods.COMDLG_FILTERSPEC
                    {
                        pszName = filter.Name,
                        pszSpec = string.Join(";", filter.Patterns)
                    };

                    Marshal.StructureToPtr(filterStr, filterPtr, false);
                    Marshal.Copy(filterPtr, resultArr, i * size, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(filterPtr);
                }
            }

            length = filters.Count;
            return resultArr;
        }
    }
}
