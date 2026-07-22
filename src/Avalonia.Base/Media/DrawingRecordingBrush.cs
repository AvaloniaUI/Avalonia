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
    /// Paints an area with a pre-recorded <see cref="DrawingRecording"/> tiled according
    /// to the <see cref="TileBrush"/> base properties.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="DrawingBrush"/>, which re-records its content from a
    /// <see cref="Drawing"/> each time a compositor references it, this brush reuses an
    /// existing <see cref="DrawingRecording"/>. Recordings passed in are retained for the
    /// brush's lifetime on the compositors that reference the brush — the caller remains
    /// the owner and must dispose the recording when no longer needed.
    /// </remarks>
    public sealed class DrawingRecordingBrush : TileBrush, ISceneBrush
    {
        /// <summary>
        /// Defines the <see cref="Recording"/> property.
        /// </summary>
        public static readonly StyledProperty<DrawingRecording?> RecordingProperty =
            AvaloniaProperty.Register<DrawingRecordingBrush, DrawingRecording?>(nameof(Recording));

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingRecordingBrush"/> class.
        /// </summary>
        public DrawingRecordingBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingRecordingBrush"/> class.
        /// </summary>
        /// <param name="recording">The recording to paint with.</param>
        public DrawingRecordingBrush(DrawingRecording recording)
        {
            Recording = recording;
        }

        /// <summary>
        /// Gets or sets the <see cref="DrawingRecording"/> to paint with.
        /// </summary>
        /// <remarks>
        /// Changing this property after the brush has been referenced by a
        /// compositor does not refresh already-created compositor content
        /// (matching <see cref="DrawingBrush.Drawing"/> semantics) — create a
        /// new brush instead of mutating this property.
        /// </remarks>
        public DrawingRecording? Recording
        {
            get => GetValue(RecordingProperty);
            set => SetValue(RecordingProperty, value);
        }

        ISceneBrushContent? ISceneBrush.CreateContent()
        {
            var recording = Recording;
            if (recording == null || recording.IsDisposed)
                return null;

            using var recorder = new RenderDataDrawingContext(null);
            recorder.DrawRecording(recording);
            return recorder.GetImmediateSceneBrushContent(this, null, true);
        }

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleContentBrush(c.Server);

        private InlineDictionary<Compositor, CompositionRenderData?> _renderDataDictionary;

        private protected override void OnReferencedFromCompositor(Compositor c)
        {
            _renderDataDictionary.Add(c, CreateServerContent(c));
            base.OnReferencedFromCompositor(c);
        }

        protected override void OnUnreferencedFromCompositor(Compositor c)
        {
            if (_renderDataDictionary.TryGetAndRemoveValue(c, out var content))
                content?.Dispose();
            base.OnUnreferencedFromCompositor(c);
        }

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            if (_renderDataDictionary.TryGetValue(c, out var content) && content != null)
                writer.WriteObject(new CompositionRenderDataSceneBrushContent.Properties(content.Server, null, true));
            else
                writer.WriteObject(null);
        }

        CompositionRenderData? CreateServerContent(Compositor c)
        {
            var recording = Recording;
            if (recording == null || recording.IsDisposed)
                return null;

            // A compositor-bound source recording must match this compositor; if it doesn't,
            // we cannot reuse its server state, and we render no content.
            if (recording.IsCompositorBound && recording.Compositor != c)
                return null;

            using var recorder = new RenderDataDrawingContext(c);
            recorder.DrawRecording(recording);
            return recorder.GetRenderResults();
        }
    }
}
