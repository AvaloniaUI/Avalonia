using System;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls;

internal partial class PresentationSource
{
    private readonly Func<Size> _clientSizeProvider;
    public CompositingRenderer Renderer { get; }
    IRenderer IPresentationSource.Renderer => Renderer;
    Visual IPresentationSource.RootVisual => RootVisual;
    public IHitTester HitTester => HitTesterOverride ?? Renderer;
    //TODO: Can we PLEASE get rid of this abomination in tests and use actual hit-testing engine instead?
    public IHitTester? HitTesterOverride { get; set; }
    
    public double RenderScaling => PlatformImpl?.RenderScaling ?? 1;
    public Size ClientSize => _clientSizeProvider();
    
    public void SceneInvalidated(object? sender, SceneInvalidatedEventArgs sceneInvalidatedEventArgs)
    {
        _pointerOverPreProcessor?.SceneInvalidated(sceneInvalidatedEventArgs.DirtyRect);
    }

    public PixelPoint PointToScreen(Point point) => PlatformImpl?.PointToScreen(point) ?? default;

    public Point PointToClient(PixelPoint point) => PlatformImpl?.PointToClient(point) ?? default;
}