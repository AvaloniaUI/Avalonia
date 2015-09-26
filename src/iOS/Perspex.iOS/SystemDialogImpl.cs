using Perspex.Controls.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Controls;
using Perspex.Platform;
using System.Threading.Tasks;

namespace Perspex.iOS
{
    class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            throw new NotImplementedException();
        }
    }
}
