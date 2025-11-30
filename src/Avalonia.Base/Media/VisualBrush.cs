using System;
using Avalonia.Collections.Pooled;
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
            ImmediateRenderer.Render(recorder, Visual);
            return recorder.GetImmediateSceneBrushContent(this, new(Visual.Bounds.Size), true);
        }
        
        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleContentBrush(c.Server);

        class RenderDataItem(CompositionRenderData data, Rect rect) : IDisposable
        {
            public CompositionRenderData Data { get; } = data;
            public Rect Rect { get; } = rect;
            public bool IsDirty;
            public void Dispose() => Data?.Dispose();
        }
        
        private InlineDictionary<Compositor, RenderDataItem?> _renderDataDictionary;
        
        protected override void OnUnreferencedFromCompositor(Compositor c)
        {
            if (_renderDataDictionary.TryGetAndRemoveValue(c, out var content))
                content?.Dispose();
            base.OnUnreferencedFromCompositor(c);
        }
        
        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            CompositionRenderDataSceneBrushContent.Properties? content = null;
            if (IsOnCompositor(c)) // Should always be true here, but just in case do this check
            {
                _renderDataDictionary.TryGetValue(c, out var data);
                if (data == null || data.IsDirty)
                {
                    var created = CreateServerContent(c);
                    // Dispose the old render list _after_ creating a new one to avoid unnecessary detach/attach
                    // sequence for referenced resources
                    if (data != null) 
                        data.Dispose();
                    
                    _renderDataDictionary[c] = data = created;
                }

                if (data != null)
                    content = new(data.Data.Server, data.Rect, true);
            }
            
            writer.WriteObject(content);
        }
        
        void InvalidateContent()
        {
            foreach(var item in _renderDataDictionary)
                if (item.Value != null)
                    item.Value.IsDirty = true;
            RegisterForSerialization();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            // We are supposed to be only calling this when content is actually changed,
            // but instead we are calling this on brush property change for backwards compat with 0.10.x
            InvalidateContent();
            base.OnPropertyChanged(change);
        }

        RenderDataItem? CreateServerContent(Compositor c)
        {
            if (Visual == null)
                return null;

            if (Visual is IVisualBrushInitialize initialize)
                initialize.EnsureInitialized();

            using var recorder = new RenderDataDrawingContext(c);
            ImmediateRenderer.Render(recorder, Visual);
            var renderData = recorder.GetRenderResults();
            if (renderData == null)
                return null;

            return new(renderData, new(Visual.Bounds.Size));
        }
    }
}
