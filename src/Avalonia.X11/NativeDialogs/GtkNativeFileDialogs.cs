using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using static Avalonia.X11.NativeDialogs.Glib;
using static Avalonia.X11.NativeDialogs.Gtk;
// ReSharper disable AccessToModifiedClosure
namespace Avalonia.X11.NativeDialogs
{
    class GtkSystemDialog : ISystemDialogImpl
    {
        private Task<bool> _initialized;
        private unsafe  Task<string[]> ShowDialog(string title, IWindowImpl parent, GtkFileChooserAction action,
            bool multiSelect, string initialFileName, IEnumerable<FileDialogFilter> filters, string defaultExtension, bool overwritePrompt)
        {
            IntPtr dlg;
            using (var name = new Utf8Buffer(title))
                dlg = gtk_file_chooser_dialog_new(name, IntPtr.Zero, action, IntPtr.Zero);
            UpdateParent(dlg, parent);
            if (multiSelect)
                gtk_file_chooser_set_select_multiple(dlg, true);

            gtk_window_set_modal(dlg, true);
            var tcs = new TaskCompletionSource<string[]>();
            List<IDisposable> disposables = null;

            void Dispose()
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var d in disposables) d.Dispose();
                disposables.Clear();
            }

            var filtersDic = new Dictionary<IntPtr, FileDialogFilter>();
            if(filters != null)
                foreach (var f in filters)
                {
                    var filter = gtk_file_filter_new();
                    filtersDic[filter] = f;
                    using (var b = new Utf8Buffer(f.Name))
                        gtk_file_filter_set_name(filter, b);
                    
                    foreach (var e in f.Extensions)
                        using (var b = new Utf8Buffer("*." + e))
                            gtk_file_filter_add_pattern(filter, b);

                    gtk_file_chooser_add_filter(dlg, filter);
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
                    string[] result = null;
                    if (resp == GtkResponseType.Accept)
                    {
                        var resultList = new List<string>();
                        var gs = gtk_file_chooser_get_filenames(dlg);
                        var cgs = gs;
                        while (cgs != null)
                        {
                            if (cgs->Data != IntPtr.Zero)
                                resultList.Add(Utf8Buffer.StringFromPtr(cgs->Data));
                            cgs = cgs->Next;
                        }
                        g_slist_free(gs);
                        result = resultList.ToArray();
                        
                        // GTK doesn't auto-append the extension, so we need to do that manually
                        if (action == GtkFileChooserAction.Save)
                        {
                            var currentFilter = gtk_file_chooser_get_filter(dlg);
                            filtersDic.TryGetValue(currentFilter, out var selectedFilter);
                            for (var c = 0; c < result.Length; c++)
                                result[c] = NameWithExtension(result[c], defaultExtension, selectedFilter);
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
                gtk_dialog_add_button(dlg, open, GtkResponseType.Accept);
            using (var open = new Utf8Buffer("Cancel"))
                gtk_dialog_add_button(dlg, open, GtkResponseType.Cancel);
            if (initialFileName != null)
                using (var fn = new Utf8Buffer(initialFileName))
                {
                    if (action == GtkFileChooserAction.Save)
                        gtk_file_chooser_set_current_name(dlg, fn);
                    else
                        gtk_file_chooser_set_filename(dlg, fn);
                }

            gtk_file_chooser_set_do_overwrite_confirmation(dlg, overwritePrompt);
            
            gtk_window_present(dlg);
            return tcs.Task;
        }

        string NameWithExtension(string path, string defaultExtension, FileDialogFilter filter)
        {
            var name = Path.GetFileName(path);
            if (name != null && !name.Contains("."))
            {
                if (filter?.Extensions?.Count > 0)
                {
                    if (defaultExtension != null
                        && filter.Extensions.Contains(defaultExtension))
                        return path + "." + defaultExtension.TrimStart('.');

                    var ext = filter.Extensions.FirstOrDefault(x => x != "*");
                    if (ext != null)
                        return path + "." + ext.TrimStart('.');
                }

                if (defaultExtension != null)
                    path += "." + defaultExtension.TrimStart('.');
            }

            return path;
        }

        public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            await EnsureInitialized();

            var platformImpl = parent?.PlatformImpl;

            return await await RunOnGlibThread(
                () => ShowDialog(dialog.Title, platformImpl,
                    dialog is OpenFileDialog ? GtkFileChooserAction.Open : GtkFileChooserAction.Save,
                    (dialog as OpenFileDialog)?.AllowMultiple ?? false,
                    Path.Combine(string.IsNullOrEmpty(dialog.Directory) ? "" : dialog.Directory,
                        string.IsNullOrEmpty(dialog.InitialFileName) ? "" : dialog.InitialFileName), dialog.Filters,
                    (dialog as SaveFileDialog)?.DefaultExtension, (dialog as SaveFileDialog)?.ShowOverwritePrompt ?? false));
        }

        public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            await EnsureInitialized();

            var platformImpl = parent?.PlatformImpl;

            return await await RunOnGlibThread(async () =>
            {
                var res = await ShowDialog(dialog.Title, platformImpl,
                    GtkFileChooserAction.SelectFolder, false, dialog.Directory, null, null, false);
                return res?.FirstOrDefault();
            });
        }
        
        async Task EnsureInitialized()
        {
            if (_initialized == null) _initialized = StartGtk();

            if (!(await _initialized))
                throw new Exception("Unable to initialize GTK on separate thread");
        }
        
        void UpdateParent(IntPtr chooser, IWindowImpl parentWindow)
        {
            var xid = parentWindow.Handle.Handle;
            gtk_widget_realize(chooser);
            var window = gtk_widget_get_window(chooser);
            var parent = GetForeignWindow(xid);
            if (window != IntPtr.Zero && parent != IntPtr.Zero)
                gdk_window_set_transient_for(window, parent);
        }
    }
}
