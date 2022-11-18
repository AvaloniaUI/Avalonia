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
        /// <summary>
        /// Instance FilePickerHandler
        /// </summary>
        public FilePickerHandler()
        {

        }
        /// <summary>
        /// Instance FilePickerHandler with specificated parameter
        /// </summary>
        /// <param name="title">SaveFilePicker Title</param>
        /// <param name="screenshotRoot"></param>
        public FilePickerHandler(string? title
            , string? screenshotRoot = default
            )
        {
            if (title is { })
                Title = title;
            if (screenshotRoot is { })
                ScreenshotsRoot = screenshotRoot;
        }
        /// <summary>
        /// Get the root folder where screeshots well be stored.
        /// The default root folder is [Environment.SpecialFolder.MyPictures]/Screenshots.
        /// </summary>
        public string ScreenshotsRoot { get; }
            = Conventions.DefaultScreenshotsRoot;

        /// <summary>
        /// SaveFilePicker Title
        /// </summary>
        public string Title { get; } = "Save Screenshot to ...";

        Window GetWindow(IControl control)
        {
            var window = control.VisualRoot as Window;
            var app = Application.Current;
            if (app?.ApplicationLifetime is Lifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                window = desktop.Windows.FirstOrDefault(w => w is Views.MainWindow);
            }
            return window!;
        }

        protected override async Task<Stream?> GetStream(IControl control)
        {
            var result = await GetWindow(control).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedStartLocation = new BclStorageFolder(new DirectoryInfo(ScreenshotsRoot)),
                Title = Title,
                FileTypeChoices = new FilePickerFileType[] { new FilePickerFileType("PNG") { Patterns = new string[] { "*.png" } } },
                DefaultExtension = ".png"
            });
            if (result is null)
            {
                return null;
            }
            if (!result.CanOpenWrite)
            {
                throw new InvalidOperationException("ReadOnly file was opened");
            }

            return await result.OpenWriteAsync();
        }
    }
}
