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
using Avalonia.Input;

namespace Avalonia.Browser
{
    [SupportedOSPlatform("browser")]
    internal class BrowserShare : IShareProvider
    {
        public bool CanShareAsync(IDataObject dataObject)
        {
            return dataObject is DataObject data && (data.Contains(DataFormats.Text) || data.Contains(DataFormats.FileNames));
        }

        private async Task Share(string? text)
        {
            using var jsObject = DomHelper.Create(default);
            jsObject?.SetProperty("title", $"Sending {text}");
            jsObject?.SetProperty("text", text);

            if (jsObject != null && ShareHelper.CanShare(jsObject))
            {
                ShareHelper.Share(jsObject);
            }
        }

        private async Task Share(List<IStorageFile?> files)
        {
            var fileList = new List<IStorageFile>();

            foreach(var file in files)
            {
                if(file != null)
                {
                    fileList.Add(file); 
                }
            }

            ShareHelper.shareFileList($"Sending {fileList.Count} file{(fileList.Count > 0 ? "s" : "")}", fileList.Select(f => ((JSStorageItem)f).FileHandle).ToArray());
        }

        public async Task ShareAsync(IDataObject dataObject)
        {
            if(dataObject == null)
            {
                return;
            }

            if (dataObject.Contains(DataFormats.Text))
            {
                await Share(dataObject.GetText());
            }
            else if (dataObject.Contains(DataFormats.Files))
            {
                var files = dataObject.GetFiles()?.Select(x => x as IStorageFile).Where(x => x != null);
                if (files != null)
                {
                    await Share(files.ToList());
                }
            }
        }
    }
}
