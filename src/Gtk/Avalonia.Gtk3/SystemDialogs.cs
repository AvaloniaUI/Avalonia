using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class SystemDialog : ISystemDialogImpl
    {

        unsafe static Task<string[]> ShowDialog(string title, GtkWindow parent, GtkFileChooserAction action,
            bool multiselect, string initialFileName)
        {
            GtkFileChooser dlg;
            parent = parent ?? GtkWindow.Null;
            using (var name = new Utf8Buffer(title))
                dlg = Native.GtkFileChooserDialogNew(name, parent, action, IntPtr.Zero);
            if (multiselect)
                Native.GtkFileChooserSetSelectMultiple(dlg, true);

            Native.GtkWindowSetModal(dlg, true);
            var tcs = new TaskCompletionSource<string[]>();
            List<IDisposable> disposables = null;
            Action dispose = () =>
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var d in disposables)
                    d.Dispose();
                disposables.Clear();
            };
            disposables = new List<IDisposable>
            {
                Signal.Connect<Native.D.signal_generic>(dlg, "close", delegate
                {
                    tcs.TrySetResult(null);
                    dispose();
                    return false;
                }),
                Signal.Connect<Native.D.signal_dialog_response>(dlg, "response", (_, resp, __)=>
                {
                    string[] result = null;
                    if (resp == GtkResponseType.Accept)
                    {
                        var rlst = new List<string>();
                        var gs = Native.GtkFileChooserGetFilenames(dlg);
                        var cgs = gs;
                        while (cgs != null)
                        {
                            if (cgs->Data != IntPtr.Zero)
                                rlst.Add(Utf8Buffer.StringFromPtr(cgs->Data));
                            cgs = cgs->Next;
                        }
                        Native.GSlistFree(gs);
                        result = rlst.ToArray();
                    }
                    Native.GtkWidgetHide(dlg);
                    dispose();
                    tcs.TrySetResult(result);
                    return false;
                }),
                dlg
            };
            using (var open = new Utf8Buffer("Open"))
                Native.GtkDialogAddButton(dlg, open, GtkResponseType.Accept);
            using (var open = new Utf8Buffer("Cancel"))
                Native.GtkDialogAddButton(dlg, open, GtkResponseType.Cancel);
            if(initialFileName!=null)
                using (var fn = new Utf8Buffer(initialFileName))
                    Native.GtkFileChooserSetFilename(dlg, fn);
            Native.GtkWindowPresent(dlg);
            return tcs.Task;
        }

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            return ShowDialog(dialog.Title, ((WindowBaseImpl)parent)?.GtkWidget,
                dialog is OpenFileDialog ? GtkFileChooserAction.Open : GtkFileChooserAction.Save,
                (dialog as OpenFileDialog)?.AllowMultiple ?? false,
                Path.Combine(string.IsNullOrEmpty(dialog.InitialDirectory) ? "" : dialog.InitialDirectory,
                string.IsNullOrEmpty(dialog.InitialFileName) ? "" : dialog.InitialFileName));
        }

        public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            var res = await ShowDialog(dialog.Title, ((WindowBaseImpl) parent)?.GtkWidget,
                GtkFileChooserAction.SelectFolder, false, dialog.InitialDirectory);
            return res?.FirstOrDefault();
        }
    }
}
