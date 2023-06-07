using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="Drawing"/>.
    /// </summary>
    public sealed class DrawingBrush : TileBrush, ISceneBrush
    {
        /// <summary>
        /// Defines the <see cref="Drawing"/> property.
        /// </summary>
        public static readonly StyledProperty<Drawing?> DrawingProperty =
            AvaloniaProperty.Register<DrawingBrush, Drawing?>(nameof(Drawing));
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingBrush"/> class.
        /// </summary>
        public DrawingBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public DrawingBrush(Drawing visual)
        {
            Drawing = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public Drawing? Drawing
        {
            get { return GetValue(DrawingProperty); }
            set { SetValue(DrawingProperty, value); }
        }

        ISceneBrushContent? ISceneBrush.CreateContent()
        {
            if (Drawing == null)
                return null;
            
            using var recorder = new RenderDataDrawingContext(null);
            Drawing?.Draw(recorder);
            return recorder.GetImmediateSceneBrushContent(this, null, true);
        }

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleContentBrush(c.Server);

        private InlineDictionary<Compositor, CompositionRenderDataSceneBrushContent?> _renderDataDictionary;

        private protected override void OnReferencedFromCompositor(Compositor c)
        {
            _renderDataDictionary.Add(c, CreateServerContent(c));
            base.OnReferencedFromCompositor(c);
        }

        protected override void OnUnreferencedFromCompositor(Compositor c)
        {
            if (_renderDataDictionary.TryGetAndRemoveValue(c, out var content))
                content?.RenderData.Dispose();
            base.OnUnreferencedFromCompositor(c);
        }
        
        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            if (_renderDataDictionary.TryGetValue(c, out var content))
                writer.WriteObject(content);
            else
                writer.WriteObject(null);
        }
        
        CompositionRenderDataSceneBrushContent? CreateServerContent(Compositor c)
        {
            if (Drawing == null)
                return null;
            
            using var recorder = new RenderDataDrawingContext(c);
            Drawing?.Draw(recorder);
            var renderData = recorder.GetRenderResults();
            if (renderData == null)
                return null;
            
            return new CompositionRenderDataSceneBrushContent(
                (ServerCompositionSimpleContentBrush)((ICompositionRenderResource<IBrush>)this).GetForCompositor(c),
                renderData, null, true);
        }
    }
}
