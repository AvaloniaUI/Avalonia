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
    /// Paints an area with an <see cref="Visual"/>.
    /// </summary>
    public sealed class VisualBrush : TileBrush, ISceneBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<Visual?> VisualProperty =
            AvaloniaProperty.Register<VisualBrush, Visual?>(nameof(Visual));
        

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public VisualBrush(Visual visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public Visual? Visual
        {
            get { return GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        ISceneBrushContent? ISceneBrush.CreateContent()
        {
            if (Visual == null)
                return null;

            if (Visual is IVisualBrushInitialize initialize)
                initialize.EnsureInitialized();
            
            using var recorder = new RenderDataDrawingContext(null);
            ImmediateRenderer.Render(recorder, Visual, Visual.Bounds);
            return recorder.GetImmediateSceneBrushContent(this, new(Visual.Bounds.Size), false);
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
            if (Visual == null)
                return null;

            if (Visual is IVisualBrushInitialize initialize)
                initialize.EnsureInitialized();

            using var recorder = new RenderDataDrawingContext(c);
            ImmediateRenderer.Render(recorder, Visual, Visual.Bounds);
            var renderData = recorder.GetRenderResults();
            if (renderData == null)
                return null;
            
            return new CompositionRenderDataSceneBrushContent(
                (ServerCompositionSimpleContentBrush)((ICompositionRenderResource<IBrush>)this).GetForCompositor(c),
                renderData, new(Visual.Bounds.Size), false);
        }
    }
}
