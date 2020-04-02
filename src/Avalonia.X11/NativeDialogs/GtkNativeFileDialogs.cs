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
            bool multiSelect, string initialFileName, IEnumerable<FileDialogFilter> filters)
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
            
            if(filters != null)
                foreach (var f in filters)
                {
                    var filter = gtk_file_filter_new();
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
                    gtk_file_chooser_set_filename(dlg, fn);
            gtk_window_present(dlg);
            return tcs.Task;
        }
        
        public async Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            await EnsureInitialized();
            return await await RunOnGlibThread(
                () => ShowDialog(dialog.Title, parent,
                    dialog is OpenFileDialog ? GtkFileChooserAction.Open : GtkFileChooserAction.Save,
                    (dialog as OpenFileDialog)?.AllowMultiple ?? false,
                    Path.Combine(string.IsNullOrEmpty(dialog.InitialDirectory) ? "" : dialog.InitialDirectory,
                        string.IsNullOrEmpty(dialog.InitialFileName) ? "" : dialog.InitialFileName), dialog.Filters));
        }

        public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            await EnsureInitialized();
            return await await RunOnGlibThread(async () =>
            {
                var res = await ShowDialog(dialog.Title, parent,
                    GtkFileChooserAction.SelectFolder, false, dialog.InitialDirectory, null);
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
