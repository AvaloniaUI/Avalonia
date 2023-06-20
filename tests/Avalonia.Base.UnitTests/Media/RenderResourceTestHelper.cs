using System;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

internal class RenderResourceTestHelper : IDisposable
{
    public CompositorTestServices Services { get; } = new();
    public Compositor Compositor => Services.Compositor;
    
    public void AddToCompositor(ICompositionRenderResource resource) => resource.AddRefOnCompositor(Compositor);

    public bool IsInvalidated(ICompositorSerializable resource) =>
        Compositor.UnitTestIsRegisteredForSerialization(resource);

    public void AssertExistsOnCompositor(ICompositorSerializable resource, bool exists = true)
    {
        var server = resource.TryGetServer(Compositor);
        if (exists)
            Assert.NotNull(server);
        else
            Assert.Null(server);
    }

    public static void AssertResourceInvalidation<T>(T resource, Action cb)
        where T : ICompositionRenderResource, ICompositorSerializable
    {
        using var helper = new RenderResourceTestHelper();
        helper.AssertInvalidation(resource, cb);
    }
    
    public void AssertInvalidation<T>(T resource, Action cb)
        where T : ICompositionRenderResource, ICompositorSerializable
    {
        resource.AddRefOnCompositor(Compositor);
        Assert.NotNull(resource.TryGetServer(Compositor));
        Assert.True(Compositor.UnitTestIsRegisteredForSerialization(resource));
        
        Compositor.Commit();
        Compositor.Server.Render();
        
        Assert.False(Compositor.UnitTestIsRegisteredForSerialization(resource));
        cb();
        Assert.True(Compositor.UnitTestIsRegisteredForSerialization(resource));
        resource.ReleaseOnCompositor(Compositor);
        Assert.Null(resource.TryGetServer(Compositor));
    }

    public void Dispose() => Services.Dispose();
}