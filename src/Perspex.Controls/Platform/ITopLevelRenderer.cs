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

            var platformRender = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            if(platformRender == null)
                return;

            var viewport = platformRender
                .CreateRenderer(topLevel.PlatformImpl.Handle, initialClientSize.Width, initialClientSize.Height);
            resources.Add(viewport);


            resources.Add(topLevel.GetObservable(TopLevel.ClientSizeProperty).Subscribe(clientSize =>
            {
                viewport.Resize((int) clientSize.Width, (int) clientSize.Height);
            }));
            resources.Add(queueManager.RenderNeeded.Subscribe(_
                =>
                Dispatcher.UIThread.Invoke(() => topLevel.PlatformImpl.Invalidate(new Rect(topLevel.ClientSize)))));

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
