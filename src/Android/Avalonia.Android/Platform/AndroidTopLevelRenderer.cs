using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Avalonia.Android.CanvasRendering
{
    internal class AndroidTopLevelRenderer : ITopLevelRenderer
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
            //resources.Add(queueManager.RenderNeeded.Subscribe(_
            //    =>
            //    Dispatcher.UIThread.InvokeAsync(() => topLevel.PlatformImpl.Invalidate(new Rect(topLevel.ClientSize)))));
            Action pendingInvalidation = null;
            resources.Add(queueManager.RenderNeeded.Subscribe(_ =>
            {
                if (pendingInvalidation == null)
                {
                    pendingInvalidation = () =>
                    {
                        topLevel.PlatformImpl.Invalidate(new Rect(topLevel.ClientSize));
                        pendingInvalidation = null;
                    };
                    Dispatcher.UIThread.InvokeAsync(pendingInvalidation);
                }
            }
            ));

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