using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.Controls.Platform
{
    public interface ISystemDialogImpl
    {
        Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent);
    }
}
