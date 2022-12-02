using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;

namespace Avalonia.Controls
{
    public interface IPageHost
    {
        IInsetsManager? InsetsManager { get; }

        bool SetPage(Page page);
    }
}
