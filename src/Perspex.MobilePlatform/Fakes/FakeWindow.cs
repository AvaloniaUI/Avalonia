using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Platform;

namespace Perspex.MobilePlatform.Fakes
{
    class FakeWindow : TopLevel
    {
        public FakeWindow(ITopLevelImpl impl) : base(impl)
        {
            impl.SetInputRoot(this);
        }
    }
}
