using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Platform;

namespace Perspex.Gtk
{
    using global::Gtk;
    class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            var tcs = new TaskCompletionSource<string[]>();
            var dlg = new global::Gtk.FileChooserDialog(dialog.Title, ((WindowImpl)parent),
                dialog is OpenFileDialog
                    ? FileChooserAction.Open
                    : FileChooserAction.Save,
                "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept)
            {
                SelectMultiple = (dialog as OpenFileDialog)?.AllowMultiple ?? false,
            };
            foreach (var filter in dialog.Filters)
            {
                var ffilter = new FileFilter()
                {
                    Name = filter.Name + " (" + string.Join(";", filter.Extensions.Select(e => "*." + e)) + ")"
                };
                foreach (var ext in filter.Extensions)
                    ffilter.AddPattern("*." + ext);
                dlg.AddFilter(ffilter);
            }
            dlg.SetFilename(dialog.InitialFileName);
            dlg.Modal = true;

            dlg.Response += (_, args) =>
            {
                if (args.ResponseId == ResponseType.Accept)
                    tcs.TrySetResult(dlg.Filenames);
                dlg.Hide();
                dlg.Dispose();
            };

            dlg.Close += delegate
            {
                tcs.TrySetResult(null);
                dlg.Dispose();
            };
            dlg.Show();
            return tcs.Task;
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            var tcs = new TaskCompletionSource<string>();
            var dlg = new global::Gtk.FileChooserDialog(dialog.Title, ((WindowImpl)parent),
                FileChooserAction.SelectFolder,
                "Cancel", ResponseType.Cancel,
                "Select Folder", ResponseType.Accept)
            {

            };

            dlg.Modal = true;

            dlg.Response += (_, args) =>
            {
                if (args.ResponseId == ResponseType.Accept)
                    tcs.TrySetResult(dlg.Filename);

                dlg.Hide();
                dlg.Dispose();
            };

            dlg.Close += delegate
            {
                tcs.TrySetResult(null);
                dlg.Dispose();
            };
            dlg.Show();
            return tcs.Task;
        }
    }
}
