using System;
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

        private sealed class RenderDataItem(CompositionRenderData data) : IDisposable
        {
            public CompositionRenderData Data { get; } = data;
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
                if (data is null || data.IsDirty)
                {
                    var created = CreateServerContent(c);
                    // Dispose the old render list _after_ creating a new one to avoid unnecessary detach/attach
                    // sequence for referenced resources
                    data?.Dispose();
                    _renderDataDictionary[c] = data = created;
                }

                if (data is not null)
                    content = new(data.Data.Server, null, true);
            }

            writer.WriteObject(content);
        }

        private void InvalidateContent()
        {
            foreach (var item in _renderDataDictionary)
                item.Value?.IsDirty = true;

            RegisterForSerialization();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            // Replacing the drawing changes the recorded content; mark it dirty so it's re-recorded.
            // Changes _inside_ the drawing (e.g. a mutated geometry or brush) are compositor-aware
            // resources and are propagated by the server without re-recording.
            if (change.Property == DrawingProperty)
                InvalidateContent();

            base.OnPropertyChanged(change);
        }

        private RenderDataItem? CreateServerContent(Compositor c)
        {
            if (Drawing is not { } drawing)
                return null;

            using var recorder = new RenderDataDrawingContext(c);
            drawing.Draw(recorder);
            var renderData = recorder.GetRenderResults();
            return renderData is null ? null : new RenderDataItem(renderData);
        }
    }
}
