using System;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Media
{
    [NotClientImplementable]
    public interface ISceneBrush : ITileBrush
    {
        ISceneBrushContent? CreateContent();
    }
    
    [NotClientImplementable]
    public interface ISceneBrushContent : IImmutableBrush, IDisposable
    {
        ITileBrush Brush { get; }
        Rect Rect { get; }
        void Render(IDrawingContextImpl context, Matrix? transform);
        internal bool UseScalableRasterization { get; }
    }

    internal class ImmutableSceneBrush : ImmutableTileBrush
    {
        public ImmutableSceneBrush(ITileBrush source) : base(source)
        {
        }
    }

    internal static class SceneBrushSnapshot
    {
        /// <summary>
        /// Snapshots a scene brush as its current content, or a transparent brush when it
        /// has none. The content wrapper captures the tile-brush properties, so nothing
        /// reads the live <see cref="AvaloniaObject"/> after this returns.
        /// </summary>
        public static IImmutableBrush Take(ISceneBrush brush) =>
            brush.CreateContent() is { } content
                ? new EmbeddedSceneBrushContent(content)
                : Brushes.Transparent;
    }
}
