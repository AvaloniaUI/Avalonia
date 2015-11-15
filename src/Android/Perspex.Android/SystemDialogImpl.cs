using System;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Platform;

namespace Perspex.Android
{
    internal class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            throw new NotImplementedException();
        }
    }
}