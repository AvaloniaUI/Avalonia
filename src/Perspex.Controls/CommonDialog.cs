using System.Collections.Generic;
using System.Threading.Tasks;
using Perspex.Controls.Platform;
using Splat;

namespace Perspex.Controls
{
    public class CommonDialog
    {
        public string Title { get; set; }
        public List<CommonDialogFilter> Filters { get; set; } = new List<CommonDialogFilter>();
        public string InitialFileName { get; set; }
        public string InitialDirectory { get; set; }
        public bool AllowMultiple { get; set; }
        public string DefaultExtension { get; set; }
        public CommonDialogAction Action { get; set; }


        public Task<string[]> ShowAsync(Window window = null) 
            => Locator.Current.GetService<ICommonDialogImpl>().ShowAsync(this, window?.PlatformImpl);
    }

    public enum CommonDialogAction
    {
        OpenFile,
        SaveFile
    }

    public class CommonDialogFilter
    {
        public string Name { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
    }
}
