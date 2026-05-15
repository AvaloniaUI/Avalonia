using Avalonia.Input;
using System;
using Avalonia.Layout;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls;

internal partial class PresentationSource
{
    public CompositingRenderer Renderer { get; }
    IRenderer IPresentationSource.Renderer => Renderer;
    Visual IPresentationSource.RootVisual => RootVisual;
    public IHitTester HitTester => HitTesterOverride ?? Renderer;
    //TODO: Can we PLEASE get rid of this abomination in tests and use actual hit-testing engine instead?
    public IHitTester? HitTesterOverride { get; set; }
    
    public double RenderScaling { get; private set; } = 1.0;
    public Size ClientSize => PlatformImpl?.ClientSize ?? default;
    
    public void SceneInvalidated(object? sender, SceneInvalidatedEventArgs sceneInvalidatedEventArgs)
    {
        _pointerOverPreProcessor?.SceneInvalidated(sceneInvalidatedEventArgs.DirtyRect);
    }

    public PixelPoint? PointToScreen(Point point) => PlatformImpl?.PointToScreen(point);

    public Point? PointToClient(PixelPoint point) => PlatformImpl?.PointToClient(point);

    private void HandleScalingChanged(double scaling)
        => RenderScaling = LayoutHelper.ValidateScaling(scaling);
}
