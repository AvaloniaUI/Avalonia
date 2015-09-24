using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.Controls.Platform
{
    public interface ICommonDialogImpl
    {
        Task<string[]> ShowAsync(CommonDialog dialog, IWindowImpl parent);
    }
}
