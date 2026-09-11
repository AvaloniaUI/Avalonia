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
    public sealed class DrawingRecordingBrush : TileBrush, ISceneBrush, IMutableBrush
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

        IImmutableBrush IMutableBrush.ToImmutable() => SceneBrushSnapshot.Take(this);

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleContentBrush(c.Server);

        private sealed class RenderDataItem(CompositionRenderData data) : IDisposable
        {
            public CompositionRenderData Data { get; } = data;
            public bool IsDirty;
            public void Dispose() => Data.Dispose();
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
            if (IsOnCompositor(c))
            {
                _renderDataDictionary.TryGetValue(c, out var data);
                if (data is null || data.IsDirty)
                {
                    var created = CreateServerContent(c);
                    // Dispose the old render list _after_ creating a new one to avoid an
                    // unnecessary detach/attach sequence for referenced resources
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
            // A different recording is different content, so the per-compositor render list
            // has to be rebuilt. Mutations _inside_ a compositor-bound recording reach the
            // server on their own and need no re-recording.
            if (change.Property == RecordingProperty)
                InvalidateContent();

            base.OnPropertyChanged(change);
        }

        private RenderDataItem? CreateServerContent(Compositor c)
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
            return recorder.GetRenderResults() is { } data ? new RenderDataItem(data) : null;
        }
    }
}
