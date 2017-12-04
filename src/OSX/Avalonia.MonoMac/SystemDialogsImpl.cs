using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class SystemDialogsImpl : ISystemDialogImpl
    {

        Task<string[]> RunPanel(NSSavePanel panel, IWindowImpl parent)
        {
            var keyWindow = MonoMacPlatform.App.KeyWindow;
            var tcs = new TaskCompletionSource<string[]>();
            void OnComplete(int result)
            {
                if (result == 0)
                    tcs.SetResult(null);
                else
                {
                    if (panel is NSOpenPanel openPanel)
                        tcs.SetResult(openPanel.Filenames);
                    else
                        tcs.SetResult(new[] { panel.Filename });
                }
                panel.OrderOut(panel);
                keyWindow?.MakeKeyAndOrderFront(keyWindow);
                MonoMacPlatform.App.ActivateIgnoringOtherApps(true);
                panel.Dispose();
            }

            if (parent != null)
            {
                var window = (WindowImpl)parent;
                panel.BeginSheet(window.Window, OnComplete);
            }
            else
                panel.Begin(OnComplete);
            return tcs.Task;
        }

        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            /* NOTES
             * DefaultFileExtension is not supported
             * Named filters are not supported
            */
            NSSavePanel panel;
            if (dialog is OpenFileDialog openDialog)
            {
                var openPanel = new NSOpenPanel();
                panel = openPanel;
                
                openPanel.AllowsMultipleSelection = openDialog.AllowMultiple;
            }
            else
                panel = new NSSavePanel();
            panel.Title = panel.Title;
            if (dialog.InitialDirectory != null)
                panel.Directory = dialog.InitialDirectory;
            if (dialog.InitialFileName != null)
                panel.NameFieldStringValue = dialog.InitialFileName;
            if (dialog.Filters?.Count > 0)
                panel.AllowedFileTypes = dialog.Filters.SelectMany(f => f.Extensions).Distinct().ToArray();


            return RunPanel(panel, parent);
        }



        public async Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
        {
            var panel = new NSOpenPanel
            {
                Title = dialog.Title,
                CanChooseDirectories = true,
                CanCreateDirectories = true,
                CanChooseFiles = false
            };
            if (dialog.DefaultDirectory != null)
                panel.Directory = dialog.DefaultDirectory;
            return (await RunPanel(panel, parent))?.FirstOrDefault();
        }
    }
}
