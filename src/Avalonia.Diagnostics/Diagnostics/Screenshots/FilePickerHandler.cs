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
        /// Get or sets the root folder where screeshots well be stored.
        /// The default root folder is [Environment.SpecialFolder.MyPictures]/Screenshots.
        /// </summary>
        public string ScreenshotsRoot { get; set; }
            = Convetions.DefaultScreenshotsRoot;

        /// <summary>
        /// Get or sets conventin for screenshot fileName.
        /// For known default screen shot file name convection see <see href="https://github.com/AvaloniaUI/Avalonia/issues/4743">GH-4743</see>.
        /// </summary>
        public Func<IControl, string, string> ScreenshotFileNameConvention { get; set; }
            = Convetions.DefaultScreenshotFileNameConvention;

        /// <summary>
        /// SaveFilePicker Title
        /// </summary>
        public string Title { get; set; } = "Save Screenshot to ...";

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
                InitialFileName = ScreenshotFileNameConvention(control, ScreenshotsRoot)
            }.ShowAsync(GetWindow(control));
            if (result is { })
            {
                output = new FileStream(result, FileMode.Create);
            }
            return output;
        }
    }
}
