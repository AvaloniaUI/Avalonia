using System;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media
{
    [PrivateApi]
    public interface ISceneBrush : ITileBrush
    {
        ISceneBrushContent? CreateContent();
    }
    
    [PrivateApi]
    public interface ISceneBrushContent : IImmutableBrush, IDisposable
    {
        ITileBrush Brush { get; }
        Rect Rect { get; }
        [Obsolete]
        internal void Render(IDrawingContextImpl context, CompositionMatrix? transform);
        internal bool UseScalableRasterization { get; }
    }

    internal class ImmutableSceneBrush : ImmutableTileBrush
    {
        public ImmutableSceneBrush(ITileBrush source) : base(source)
        {
        }
    }
}
