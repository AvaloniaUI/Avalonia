using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Threading;

namespace Perspex.Controls.Platform
{
    public interface ITopLevelRenderer
    {
        void Attach(TopLevel topLevel);
    }


    class DefaultTopLevelRenderer : ITopLevelRenderer
    {

        public void Attach(TopLevel topLevel)
        {
            var resources = new List<IDisposable>();
            var initialClientSize = topLevel.PlatformImpl.ClientSize;


            var queueManager = ((IRenderRoot)topLevel).RenderQueueManager;

            if (queueManager == null)
                return;


            var viewport = PlatformManager.CreateRenderTarget(topLevel.PlatformImpl);
            resources.Add(viewport);
            resources.Add(queueManager.RenderNeeded.Subscribe(_
                =>
                Dispatcher.UIThread.InvokeAsync(() => topLevel.PlatformImpl.Invalidate(new Rect(topLevel.ClientSize)))));

            topLevel.PlatformImpl.Paint = rect =>
            {
                viewport.Render(topLevel);
                queueManager.RenderFinished();
            };

            topLevel.Closed += delegate
            {
                foreach (var disposable in resources)
                    disposable.Dispose();
                resources.Clear();
            };

        }
    }
}
