using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Platform
{
    public interface IWindowingPlatform
    {
        IWindowImpl CreateWindow();
        IWindowImpl CreateDesignerFriendlyWindow();
        IPopupImpl CreatePopup();
    }
}
