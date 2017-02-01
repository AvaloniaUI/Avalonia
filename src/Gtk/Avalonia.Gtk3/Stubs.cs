using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Platform;

//TODO: This file should be empty once everything is implemented

namespace Avalonia.Gtk3
{
    class SystemDialogStub : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent) => Task.FromResult(new string[0]);

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent)
            => Task.FromResult((string) null);
    }
}
