using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Screenshots
{
    /// <summary>
    /// Take a Screenshot on file
    /// </summary>
    public sealed class FileHandler : IScreenshotHandler
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

        public async Task Take(IControl control)
        {
            var filePath = ScreenshotFileNameConvention(control, ScreenshotsRoot);
            var folder = System.IO.Path.GetDirectoryName(filePath);
            if (System.IO.Directory.Exists(folder) == false)
            {
                await Task.Run(new Action(() => System.IO.Directory.CreateDirectory(folder!)));
            }
            using var output = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
            control.RenderTo(output);
            await output.FlushAsync();
        }
    }
}
