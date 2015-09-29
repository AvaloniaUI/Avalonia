using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.MobilePlatform.Fakes
{
    class FakePlatformHandle : IPlatformHandle
    {
        public FakePlatformHandle(MobileTopLevel topLevel)
        {
            TopLevel = topLevel;
        }

        public IntPtr Handle => IntPtr.Zero;
        public string HandleDescriptor => "MobilePlatformVirtualHandle";
        public MobileTopLevel TopLevel { get; }
    }
}
