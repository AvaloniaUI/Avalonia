using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.TinyWM.Fakes
{
    class FakePlatformHandle : IPlatformHandle
    {
        public FakePlatformHandle(TopLevelImpl topLevel)
        {
            TopLevel = topLevel;
        }

        public IntPtr Handle => IntPtr.Zero;
        public string HandleDescriptor => "TinyWMVirtualHandle";
        public TopLevelImpl TopLevel { get; }
    }
}
