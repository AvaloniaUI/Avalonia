using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Diagnostics.Screenshots
{
    /// <summary>
    /// Show a FileSavePicker to select where save screenshot
    /// </summary>
    public sealed class FilePickerHandler : BaseRenderToStreamHandler
    {
        private readonly string _title;
        private readonly string _screenshotRoot;

        /// <summary>
        /// Instance FilePickerHandler
        /// </summary>
        public FilePickerHandler() : this(null, null)
        {

        }

        /// <summary>
        /// Instance FilePickerHandler with specificated parameter
        /// </summary>
        /// <param name="title">SaveFilePicker Title</param>
        /// <param name="screenshotRoot"></param>
        public FilePickerHandler(
            string? title,
            string? screenshotRoot = default)
        {
            _title = title ?? "Save Screenshot to ...";
            _screenshotRoot = screenshotRoot ?? Conventions.DefaultScreenshotsRoot;
        }

        private static TopLevel GetTopLevel(Control control)
        {
            // If possible, use devtools main window.
            TopLevel? devToolsTopLevel = null;
            if (Application.Current?.ApplicationLifetime is Lifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                devToolsTopLevel = desktop.Windows.FirstOrDefault(w => w is Views.MainWindow);
            }

            return devToolsTopLevel ?? TopLevel.GetTopLevel(control)
                ?? throw new InvalidOperationException("No TopLevel is available.");
        }

        protected override async Task<Stream?> GetStream(Control control)
        {
            var storageProvider = GetTopLevel(control).StorageProvider;
            var defaultFolder = await storageProvider.TryGetFolderFromPathAsync(_screenshotRoot)
                                ?? await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures);

            var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedStartLocation = defaultFolder,
                Title = _title,
                FileTypeChoices = new [] { FilePickerFileTypes.ImagePng },
                DefaultExtension = ".png"
            });
            if (result is null)
            {
                return null;
            }

            return await result.OpenWriteAsync();
        }
    }
}
