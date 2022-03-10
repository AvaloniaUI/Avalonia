using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Android
{
    internal class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            throw new NotImplementedException();
        }

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            throw new NotImplementedException();
        }
    }
}
