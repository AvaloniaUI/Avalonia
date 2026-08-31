using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionBrushTests : ScopedTestBase
{
    [Fact]
    public void Replacing_The_Gradient_Stop_List_After_A_Commit_Should_Reach_The_Server()
    {
        using var services = new CompositorTestServices();
        var compositor = services.Compositor;

        var brush = compositor.CreateLinearGradientBrush();
        brush.GradientStops.Add(compositor.CreateGradientStop(0, Colors.Red));
        services.RunJobs();

        Assert.Single(brush.Server.GradientStops);

        brush.GradientStops = new List<IGradientStop>
        {
            compositor.CreateGradientStop(0, Colors.Red),
            compositor.CreateGradientStop(1, Colors.Blue),
        };
        services.RunJobs();

        Assert.Equal(2, brush.Server.GradientStops.Count);
    }

    [Fact]
    public void Changing_SpreadMethod_After_A_Commit_Should_Reach_The_Server()
    {
        using var services = new CompositorTestServices();
        var compositor = services.Compositor;

        var brush = compositor.CreateLinearGradientBrush();
        brush.GradientStops.Add(compositor.CreateGradientStop(0, Colors.Red));
        services.RunJobs();

        Assert.Equal(GradientSpreadMethod.Pad, brush.Server.SpreadMethod);

        brush.SpreadMethod = GradientSpreadMethod.Repeat;
        services.RunJobs();

        Assert.Equal(GradientSpreadMethod.Repeat, brush.Server.SpreadMethod);
    }

    [Fact]
    public void Mutable_Gradient_Stops_Should_Be_Snapshotted_For_The_Server()
    {
        using var services = new CompositorTestServices();
        var compositor = services.Compositor;

        var mutableStop = new GradientStop(Colors.Red, 0);
        var brush = compositor.CreateLinearGradientBrush();
        brush.GradientStops.Add(mutableStop);
        services.RunJobs();

        // The render thread reads the server list at replay time, so a mutable
        // UI-thread stop must not cross the batch by reference.
        var serverStop = Assert.Single(brush.Server.GradientStops);
        Assert.NotSame(mutableStop, serverStop);
        Assert.Equal(Colors.Red, serverStop.Color);
        Assert.Equal(0, serverStop.Offset);
    }

    [Fact]
    public void Using_A_Composition_Brush_With_A_Foreign_Compositor_Should_Throw()
    {
        using var services = new CompositorTestServices();

        var brush = services.Compositor.CreateSolidColorBrush(Colors.Red);

        var foreign = new Compositor(RenderLoop.FromTimer(services.Timer), null,
            true, new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);

        // A composition brush's server object belongs to its own compositor's
        // render loop; handing it to another compositor's stream would let two
        // render threads race over one resource.
        Assert.Throws<InvalidOperationException>(() => brush.GetServer(foreign));

        // A transient context without a compositor keeps the client brush and
        // draws its static values, so no affinity applies there.
        Assert.Same(brush, brush.GetServer(null));
    }
}
