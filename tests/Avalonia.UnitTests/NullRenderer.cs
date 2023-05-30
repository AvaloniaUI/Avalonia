#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Rendering;

namespace Avalonia.UnitTests;

public sealed class NullRenderer : IRenderer
{
    public RendererDiagnostics Diagnostics { get; } = new();

    event EventHandler<SceneInvalidatedEventArgs>? IRenderer.SceneInvalidated
    {
        add { }
        remove { }
    }


    public NullRenderer()
    {
    }

    public void AddDirty(Visual visual)
    {
    }

    public void Dispose()
    {
    }

    public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
        => Enumerable.Empty<Visual>();

    public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
        => null;

    public void Paint(Rect rect)
    {
    }

    public void RecalculateChildren(Visual visual)
    {
    }

    public void Resized(Size size)
    {
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }

    public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
        => new((object?) null);
}
