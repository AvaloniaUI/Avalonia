using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Browser
{
    [SupportedOSPlatform("browser")]
    internal class BrowserShare : IShareProvider
    {
        public async Task Share(string text)
        {
            using var jsObject = DomHelper.Create(default);
            jsObject?.SetProperty("title", $"Sending {text}");
            jsObject?.SetProperty("text", text);

            if (jsObject != null && ShareHelper.CanShare(jsObject))
            {
                ShareHelper.Share(jsObject);
            }
        }

        public async Task Share(IStorageFile file)
        {
            await Share(new[] { file });
        }

        public async Task Share(IList<IStorageFile> files)
        {
            ShareHelper.shareFileList($"Sending {files.Count} file{( files.Count > 0 ? "s" : "")}", files.Select(f => ((JSStorageItem)f).FileHandle).ToArray());
        }

        public async Task Share(Stream stream, string tempName = "")
        {
            // Currently, there's no way to save a file without user input, and sharing a file in memory is not permitted.
            return;
        }
    }
}
