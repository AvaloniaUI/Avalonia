using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Moq;

namespace Avalonia.UnitTests
{
    public static class RendererMocks
    {
        internal static Mock<IRenderer> CreateRenderer()
        {
            var renderer = new Mock<IRenderer>();
            renderer.SetupGet(r => r.Diagnostics).Returns(new RendererDiagnostics());
            return renderer;
        }

        public static Compositor CreateDummyCompositor() =>
            new(new RenderLoop(new CompositorTestServices.ManualRenderTimer()), null, false,
                new CompositionCommitScheduler(), true, Dispatcher.UIThread);

        class CompositionCommitScheduler : ICompositorScheduler
        {
            public void CommitRequested(Compositor compositor)
            {
                if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() == null)
                    return;
                
                Dispatcher.UIThread.Post(() => compositor.Commit(), DispatcherPriority.AfterRender);
            }
        }
    }
}
