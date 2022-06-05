using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Tmds.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : ISystemDialogImpl
    {
        private readonly IFileChooser _fileChooser;

        internal static DBusSystemDialog? TryCreate()
        {
            var fileChooser = DBusHelper.Connection?.CreateProxy<IFileChooser>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            if (fileChooser is null)
                return null;
            try
            {
                fileChooser.GetVersionAsync().GetAwaiter().GetResult();
                return new DBusSystemDialog(fileChooser);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)?.Log(null, $"Unable to connect to org.freedesktop.portal.Desktop: {e.Message}");
                return null;
            }
        }

        private DBusSystemDialog(IFileChooser fileChooser)
        {
            _fileChooser = fileChooser;
        }

        public async Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            var parentWindow = $"x11:{parent.PlatformImpl!.Handle.Handle.ToString("X")}";
            ObjectPath objectPath;
            var options = new Dictionary<string, object>();
            if (dialog.Filters is not null)
                options.Add("filters", ParseFilters(dialog));

            switch (dialog)
            {
                case OpenFileDialog openFileDialog:
                    options.Add("multiple", openFileDialog.AllowMultiple);
                    objectPath = await _fileChooser.OpenFileAsync(parentWindow, openFileDialog.Title ?? string.Empty, options);
                    break;
                case SaveFileDialog saveFileDialog:
                    if (saveFileDialog.InitialFileName is not null)
                        options.Add("current_name", saveFileDialog.InitialFileName);
                    if (saveFileDialog.Directory is not null)
                        options.Add("current_folder", Encoding.UTF8.GetBytes(saveFileDialog.Directory));
                    objectPath = await _fileChooser.SaveFileAsync(parentWindow, saveFileDialog.Title ?? string.Empty, options);
                    break;
            }

            var request = DBusHelper.Connection!.CreateProxy<IRequest>("org.freedesktop.portal.Request", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(x => tsc.SetResult(x.results["uris"] as string[]), tsc.SetException);
            var uris = await tsc.Task;
            if (uris is null)
                return null;
            for (var i = 0; i < uris.Length; i++)
                uris[i] = new Uri(uris[i]).AbsolutePath;
            return uris;
        }

        public async Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            var parentWindow = $"x11:{parent.PlatformImpl!.Handle.Handle.ToString("X")}";
            var options = new Dictionary<string, object>
            {
                { "directory", true }
            };
            var objectPath = await _fileChooser.OpenFileAsync(parentWindow, dialog.Title ?? string.Empty, options);
            var request = DBusHelper.Connection!.CreateProxy<IRequest>("org.freedesktop.portal.Request", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(x => tsc.SetResult(x.results["uris"] as string[]), tsc.SetException);
            var uris = await tsc.Task;
            if (uris is null)
                return null;
            return uris.Length != 1 ? string.Empty : new Uri(uris[0]).AbsolutePath;
        }

        private static (string name, (uint style, string extension)[])[] ParseFilters(FileDialog dialog)
        {
            var filters = new (string name, (uint style, string extension)[])[dialog.Filters!.Count];
            for (var i = 0; i < filters.Length; i++)
            {
                var extensions = dialog.Filters[i].Extensions.Select(static x => (0u, x)).ToArray();
                filters[i] = (dialog.Filters[i].Name ?? string.Empty, extensions);
            }

            return filters;
        }
    }
}
