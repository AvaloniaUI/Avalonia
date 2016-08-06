using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform
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
                try
                {
                    viewport.Render(topLevel);
                }
                catch (RenderTargetCorruptedException ex)
                {
                    Logging.Logger.Error("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                    viewport.Dispose();
                    resources.Remove(viewport);
                    viewport = PlatformManager.CreateRenderTarget(topLevel.PlatformImpl);
                    resources.Add(viewport);
                    topLevel.PlatformImpl.Paint(rect); // Retry painting
                }
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
