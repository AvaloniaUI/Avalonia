using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Media;
using Perspex.MobilePlatform.Fakes;
using Perspex.Platform;

namespace Perspex.MobilePlatform
{
    class MobileRenderInterfaceDecorator : PlatformRenderInterfaceDecorator
    {
        public MobileRenderInterfaceDecorator() : base(Platform.NativeRenderInterface)
        {
        }

        public override IRenderer CreateRenderer(IPlatformHandle handle, double width, double height) 
            => ((FakePlatformHandle) handle).TopLevel.Renderer;
    }
}
