#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorDisposalTests : CompositorTestsBase
{
    [Fact]
    public void Disposing_Renderer_Should_Dispose_ServerCompositionTarget_When_Running_In_Background()
    {
        using var services = new CompositorTestServices(runsInBackground: true);

        var disposed = false;

        _ = Task.Run(async () =>
        {
            // ReSharper disable once AccessToModifiedClosure, AccessToDisposedClosure
            while (!Volatile.Read(ref disposed))
            {
                services.Timer.TriggerTick();
                await Task.Delay(10);
            }
        });

        services.Renderer.Dispose();
        Volatile.Write(ref disposed, true);

        Assert.True(services.Renderer.CompositionTarget.Server.IsDisposed);
    }

    [Fact]
    public void Disposing_Renderer_Should_Dispose_ServerCompositionTarget_When_Running_In_Foreground()
    {
        using var services = new CompositorTestServices(runsInBackground: false);

        services.Renderer.Dispose();

        Assert.True(services.Renderer.CompositionTarget.Server.IsDisposed);
    }
}
