using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.MobilePlatform.Fakes;
using Perspex.Platform;

namespace Perspex.MobilePlatform
{
    class FakeRenderer : IRenderer
    {
        public IVisual CapturedVisual { get; private set; }
        public void Dispose()
        {
            
        }

        public int RenderCount { get; }
        public void Render(IVisual visual, IPlatformHandle handle)
        {
            if (CapturedVisual != null && CapturedVisual != visual)
            {
                throw new InvalidOperationException("In this mode visual is set for the window forever");
            }
            CapturedVisual = visual;
            Platform.Scene.RenderRequestedBy(((FakePlatformHandle) handle).TopLevel);
        }

        public void Resize(int width, int height)
        {
        }
    }
}
