#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using static Avalonia.X11.NativeDialogs.Glib;
using static Avalonia.X11.NativeDialogs.Gtk;

namespace Avalonia.X11.NativeDialogs
{
    internal class GtkSystemDialog : BclStorageProvider
    {
        private static Task<bool>? _initialized;
        private readonly X11Window _window;

        private GtkSystemDialog(X11Window window)
        {
            _window = window;
        }

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => true;

        internal static async Task<IStorageProvider?> TryCreate(X11Window window)
        {
            _initialized ??= StartGtk();

            return await _initialized ? new GtkSystemDialog(window) : null;
        }

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            return await await RunOnGlibThread(async () =>
            {
                var res = await ShowDialog(options.Title, _window, GtkFileChooserAction.Open,
                    options.AllowMultiple, options.SuggestedStartLocation, null, options.FileTypeFilter, null, false)
                    .ConfigureAwait(false);
                return res?.Select(f => new BclStorageFile(new FileInfo(f))).ToArray() ?? Array.Empty<IStorageFile>();
            });
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            return await await RunOnGlibThread(async () =>
            {
                var res = await ShowDialog(options.Title, _window, GtkFileChooserAction.SelectFolder,
                    options.AllowMultiple, options.SuggestedStartLocation, null,
                    null, null, false).ConfigureAwait(false);
                return res?.Select(f => new BclStorageFolder(new DirectoryInfo(f))).ToArray() ?? Array.Empty<IStorageFolder>();
            });
        }
        
        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            return await await RunOnGlibThread(async () =>
            {
                var res = await ShowDialog(options.Title, _window, GtkFileChooserAction.Save,
                    false, options.SuggestedStartLocation, options.SuggestedFileName, options.FileTypeChoices, options.DefaultExtension, options.ShowOverwritePrompt ?? false)
                    .ConfigureAwait(false);
                return res?.FirstOrDefault() is { } file
                    ? new BclStorageFile(new FileInfo(file))
                    : null;
            });
        }

        private unsafe Task<string[]?> ShowDialog(string? title, IWindowImpl parent, GtkFileChooserAction action,
            bool multiSelect, IStorageFolder? initialFolder, string? initialFileName,
            IEnumerable<FilePickerFileType>? filters, string? defaultExtension, bool overwritePrompt)
        {
            IntPtr dlg;
            using (var name = new Utf8Buffer(title))
            {
                dlg = gtk_file_chooser_dialog_new(name, IntPtr.Zero, action, IntPtr.Zero);
            }

            UpdateParent(dlg, parent);
            if (multiSelect)
            {
                gtk_file_chooser_set_select_multiple(dlg, true);
            }

            gtk_window_set_modal(dlg, true);
            var tcs = new TaskCompletionSource<string[]?>();
            List<IDisposable>? disposables = null;

            void Dispose()
            {
                foreach (var d in disposables!)
                {
                    d.Dispose();
                }

                disposables.Clear();
            }

            var filtersDic = new Dictionary<IntPtr, FilePickerFileType>();
            if (filters != null)
            {
                foreach (var f in filters)
                {
                    if (f.Patterns?.Any() == true || f.MimeTypes?.Any() == true)
                    {
                        var filter = gtk_file_filter_new();
                        filtersDic[filter] = f;
                        using (var b = new Utf8Buffer(f.Name))
                        {
                            gtk_file_filter_set_name(filter, b);
                        }

                        if (f.Patterns is not null)
                        {
                            foreach (var e in f.Patterns)
                            {
                                using (var b = new Utf8Buffer(e))
                                {
                                    gtk_file_filter_add_pattern(filter, b);
                                }
                            }
                        }

                        if (f.MimeTypes is not null)
                        {
                            foreach (var e in f.MimeTypes)
                            {
                                using (var b = new Utf8Buffer(e))
                                {
                                    gtk_file_filter_add_mime_type(filter, b);
                                }
                            }
                        }

                        gtk_file_chooser_add_filter(dlg, filter);
                    }
                }
            }

            disposables = new List<IDisposable>
            {
                ConnectSignal<signal_generic>(dlg, "close", delegate
                {
                    tcs.TrySetResult(null);
                    Dispose();
                    return false;
                }),
                ConnectSignal<signal_dialog_response>(dlg, "response", (_, resp, __) =>
                {
                    string[]? result = null;
                    if (resp == GtkResponseType.Accept)
                    {
                        var resultList = new List<string>();
                        var gs = gtk_file_chooser_get_filenames(dlg);
                        var cgs = gs;
                        while (cgs != null)
                        {
                            if (cgs->Data != IntPtr.Zero
                                && Utf8Buffer.StringFromPtr(cgs->Data) is string str) { resultList.Add(str); } cgs = cgs->Next;
                        }
                        g_slist_free(gs);
                        result = resultList.ToArray();

                        // GTK doesn't auto-append the extension, so we need to do that manually
                        if (action == GtkFileChooserAction.Save)
                        {
                            var currentFilter = gtk_file_chooser_get_filter(dlg);
                            filtersDic.TryGetValue(currentFilter, out var selectedFilter);
                            for (var c = 0; c < result.Length; c++) { result[c] = StorageProviderHelpers.NameWithExtension(result[c], defaultExtension, selectedFilter); }
                        }
                    }

                    gtk_widget_hide(dlg);
                    Dispose();
                    tcs.TrySetResult(result);
                    return false;
                })
            };
            using (var open = new Utf8Buffer(
                action == GtkFileChooserAction.Save ? "Save"
                : action == GtkFileChooserAction.SelectFolder ? "Select"
                : "Open"))
            {
                gtk_dialog_add_button(dlg, open, GtkResponseType.Accept);
            }

            using (var open = new Utf8Buffer("Cancel"))
            {
                gtk_dialog_add_button(dlg, open, GtkResponseType.Cancel);
            }

            var folderLocalPath = initialFolder?.TryGetLocalPath();
            if (folderLocalPath is not null)
            {
                using var dir = new Utf8Buffer(folderLocalPath);
                gtk_file_chooser_set_current_folder(dlg, dir);
            }

            if (initialFileName != null)
            {
                // gtk_file_chooser_set_filename() expects full path
                using var fn = action == GtkFileChooserAction.Open
                    ? new Utf8Buffer(Path.Combine(folderLocalPath ?? "", initialFileName))
                    : new Utf8Buffer(initialFileName);

                if (action == GtkFileChooserAction.Save)
                {
                    gtk_file_chooser_set_current_name(dlg, fn);
                }
                else
                {
                    gtk_file_chooser_set_filename(dlg, fn);
                }
            }

            gtk_file_chooser_set_do_overwrite_confirmation(dlg, overwritePrompt);

            gtk_window_present(dlg);
            return tcs.Task;
        }

        private static void UpdateParent(IntPtr chooser, IWindowImpl parentWindow)
        {
            var xid = parentWindow.Handle.Handle;
            gtk_widget_realize(chooser);
            var window = gtk_widget_get_window(chooser);
            var parent = GetForeignWindow(xid);
            if (window != IntPtr.Zero && parent != IntPtr.Zero)
            {
                gdk_window_set_transient_for(window, parent);
            }
        }
    }
}
