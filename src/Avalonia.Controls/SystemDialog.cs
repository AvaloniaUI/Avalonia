using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;

namespace Avalonia.Controls
{
    public abstract class FileDialog : FileSystemDialog
    {
        public List<FileDialogFilter> Filters { get; set; } = new List<FileDialogFilter>();
        public string InitialFileName { get; set; }        
    }

    public abstract class FileSystemDialog : SystemDialog
    {
        public string InitialDirectory { get; set; }
    }

    public class SaveFileDialog : FileDialog
    {
        public string DefaultExtension { get; set; }

        public async Task<string> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            return ((await AvaloniaLocator.Current.GetService<ISystemDialogImpl>()
                 .ShowFileDialogAsync(this, parent?.PlatformImpl)) ??
             Array.Empty<string>()).FirstOrDefault();
        }
    }

    public class OpenFileDialog : FileDialog
    {
        public bool AllowMultiple { get; set; }

        public Task<string[]> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            return AvaloniaLocator.Current.GetService<ISystemDialogImpl>().ShowFileDialogAsync(this, parent?.PlatformImpl);
        }
    }

    public class OpenFolderDialog : FileSystemDialog
    {
        public string DefaultDirectory { get; set; }

        public Task<string> ShowAsync(Window parent)
        {
            if(parent == null)
                throw new ArgumentNullException(nameof(parent));
            return AvaloniaLocator.Current.GetService<ISystemDialogImpl>().ShowFolderDialogAsync(this, parent?.PlatformImpl);
        }
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
