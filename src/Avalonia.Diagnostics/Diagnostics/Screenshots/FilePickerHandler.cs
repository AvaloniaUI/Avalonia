using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
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
            = Convetions.DefaultScreenshotsRoot;

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

        protected async override Task<Stream?> GetStream(IControl control)
        {
            Stream? output = default;
            var result = await new SaveFileDialog()
            {
                Title = Title,
                Filters = new() { new FileDialogFilter() { Name = "PNG", Extensions = new() { "png" } } },
                Directory = ScreenshotsRoot,
            }.ShowAsync(GetWindow(control));
            if (!string.IsNullOrWhiteSpace(result))
            {
                var foldler = Path.GetDirectoryName(result);
                // Directory information for path, or null if path denotes a root directory or is
                // null. Returns System.String.Empty if path does not contain directory information.
                if (!string.IsNullOrWhiteSpace(foldler))
                {
                    if (!Directory.Exists(foldler))
                    {
                        Directory.CreateDirectory(foldler);
                    }
                    output = new FileStream(result, FileMode.Create);
                }
            }
            return output;
        }
    }
}
