using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perspex.Controls.Platform;

namespace Perspex.Controls
{
    public abstract class FileDialog : SystemDialog
    {
        public List<FileDialogFilter> Filters { get; set; } = new List<FileDialogFilter>();
        public string InitialFileName { get; set; }
        public string InitialDirectory { get; set; }
    }


    public class SaveFileDialog : FileDialog
    {
        public string DefaultExtension { get; set; }

        public async Task<string> ShowAsync(Window window = null)
            =>
                ((await PerspexLocator.Current.GetService<ISystemDialogImpl>().ShowFileDialogAsync(this, window?.PlatformImpl)) ??
                 new string[0]).FirstOrDefault();
    }

    public class OpenFileDialog : FileDialog
    {
        public bool AllowMultiple { get; set; }

        public Task<string[]> ShowAsync(Window window = null)
            => PerspexLocator.Current.GetService<ISystemDialogImpl>().ShowFileDialogAsync(this, window?.PlatformImpl);
    }

    public abstract class SystemDialog
    {
        public string Title { get; set; }
    }

    public class FileDialogFilter
    {
        public string Name { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
    }
}
