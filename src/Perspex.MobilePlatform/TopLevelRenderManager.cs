using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Rendering;

namespace Perspex.MobilePlatform
{
    class TopLevelRenderManager : ITopLevelRenderer
    {
        public void Attach(TopLevel topLevel)
        {
            var impl= ((MobileTopLevel)topLevel.PlatformImpl);
            impl.TopLevel = topLevel;
            var queueManager = ((IRenderRoot) topLevel).RenderQueueManager;
            queueManager.RenderNeeded.Subscribe(_ =>
            {
                queueManager.RenderFinished();
                Platform.Scene.RenderRequestedBy(impl);
            });
        }
    }
}
