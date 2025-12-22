using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;
using MicroCom.Runtime;
using Avalonia.Logging;

namespace Avalonia.Win32
{
    internal class Win32StorageProvider(WindowImpl windowImpl) : BclStorageProvider
    {
        private const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

        private const FILEOPENDIALOGOPTIONS DefaultDialogOptions =
            FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
            FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE | FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
            FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => true;

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            var (folders, _) = await ShowFilePicker(
                true, true,
                options.AllowMultiple, false,
                options.Title, options.SuggestedFileName, null, options.SuggestedStartLocation, null, null,
                f => new BclStorageFolder(new DirectoryInfo(f)))
                .ConfigureAwait(false);
            return folders;
        }

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            var (files, _) = await ShowFilePicker(
                true, false,
                options.AllowMultiple, false,
                options.Title, options.SuggestedFileName, options.SuggestedFileType, options.SuggestedStartLocation,
                null, options.FileTypeFilter,
                f => new BclStorageFile(new FileInfo(f)))
                .ConfigureAwait(false);
            return files;
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var (files, _) = await ShowFilePicker(
                false, false,
                false, options.ShowOverwritePrompt,
                options.Title, options.SuggestedFileName, options.SuggestedFileType, options.SuggestedStartLocation,
                options.DefaultExtension, options.FileTypeChoices,
                f => new BclStorageFile(new FileInfo(f)))
                .ConfigureAwait(false);
            return files.Count > 0 ? files[0] : null;
        }

        public override async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
        {
            var (files, index) = await ShowFilePicker(
                    false, false,
                    false, options.ShowOverwritePrompt,
                    options.Title, options.SuggestedFileName, options.SuggestedFileType, options.SuggestedStartLocation,
                    options.DefaultExtension, options.FileTypeChoices,
                    f => new BclStorageFile(new FileInfo(f)))
                .ConfigureAwait(false);
            var file = files.Count > 0 ? files[0] : null;
            var selectedFileType = options.FileTypeChoices?.Count > 0
                                   && (index > 0 && index <= options.FileTypeChoices.Count) ?
                options.FileTypeChoices[index - 1] :
                null;

            return new SaveFilePickerResult { File = file, SelectedFileType = selectedFileType };
        }

        private unsafe Task<(IReadOnlyList<TStorageItem> items, int typeIndex)> ShowFilePicker<TStorageItem>(
            bool isOpenFile,
            bool openFolder,
            bool allowMultiple,
            bool? showOverwritePrompt,
            string? title,
            string? suggestedFileName,
            FilePickerFileType? suggestedFileType,
            IStorageFolder? folder,
            string? defaultExtension,
            IReadOnlyList<FilePickerFileType>? filters,
            Func<string, TStorageItem> convert)
            where TStorageItem : IStorageItem
        {
            return Task.Factory.StartNew(() =>
            {
                IReadOnlyList<TStorageItem> result = [];
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

                    defaultExtension ??= string.Empty;

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
                                // FileTypeIndex is one based, not zero based.
                                frm.SetFileTypeIndex(1);
                            }
                        }
                    }

                    if (suggestedFileType != null &&
                        filters?.IndexOf(suggestedFileType) is { } fi and > -1)
                    { 
                        frm.SetFileTypeIndex((uint)(fi + 1));
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

                    var showResult = frm.Show(windowImpl.Handle.Handle);

                    var typeIndex = (int)frm.FileTypeIndex;

                    if ((uint)showResult == (uint)UnmanagedMethods.HRESULT.E_CANCELLED)
                    {
                        return (result, typeIndex);
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
                        result = [convert(singleResult)];
                    }

                    return (result, typeIndex);
                }
                catch (COMException ex)
                {
                    var message = new Win32Exception(ex.HResult).Message;
                    throw new COMException(message, ex);
                }
            }, TaskCreationOptions.LongRunning);
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
            return null;
        }

        private byte[] FiltersToPointer(IReadOnlyList<FilePickerFileType>? filters, out int length)
        {
            if (filters is not { Count: > 0 })
            {
                filters = [FilePickerFileTypes.All];
            }

            var size = Marshal.SizeOf<UnmanagedMethods.COMDLG_FILTERSPEC>();
            var resultArr = new byte[size * filters.Count];
            length = filters.Count;

            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                if (filter.Patterns is not { Count: > 0 })
                {
                    length--;
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(this, $"Skipping invalid {nameof(FilePickerFileType)} '{filter.Name ?? "[unnamed]"}': no patterns defined.");
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

            return resultArr;
        }
    }
}
