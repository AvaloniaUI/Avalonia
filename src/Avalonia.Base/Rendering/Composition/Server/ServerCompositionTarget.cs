using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of the <see cref="CompositionTarget"/>
    /// That's the place where we update visual transforms, track dirty rects and actually do rendering
    /// </summary>
    internal partial class ServerCompositionTarget : IDisposable
    {
        private readonly ServerCompositor _compositor;
        private readonly Func<IEnumerable<object>> _surfaces;
        private readonly DiagnosticTextRenderer? _diagnosticTextRenderer;
        private static long s_nextId = 1;
        private IRenderTarget? _renderTarget;
        private FpsCounter? _fpsCounter;
        private FrameTimeGraph? _renderTimeGraph;
        private FrameTimeGraph? _layoutTimeGraph;
        private Rect _dirtyRect;
        private readonly Random _random = new();
        private Size _layerSize;
        private IDrawingContextLayerImpl? _layer;
        private bool _redrawRequested;
        private bool _disposed;
        private readonly HashSet<ServerCompositionVisual> _attachedVisuals = new();
        private readonly Queue<ServerCompositionVisual> _adornerUpdateQueue = new();

        public long Id { get; }
        public ulong Revision { get; private set; }
        public ICompositionTargetDebugEvents? DebugEvents { get; set; }
        public ReadbackIndices Readback { get; } = new();
        public int RenderedVisuals { get; set; }

        private FpsCounter? FpsCounter
            => _fpsCounter ??= _diagnosticTextRenderer != null ? new FpsCounter(_diagnosticTextRenderer) : null;

        private FrameTimeGraph? LayoutTimeGraph
            => _layoutTimeGraph ??= CreateTimeGraph("Layout");

        private FrameTimeGraph? RenderTimeGraph
            => _renderTimeGraph ??= CreateTimeGraph("Render");

        public ServerCompositionTarget(ServerCompositor compositor, Func<IEnumerable<object>> surfaces,
            DiagnosticTextRenderer? diagnosticTextRenderer)
            : base(compositor)
        {
            _compositor = compositor;
            _surfaces = surfaces;
            _diagnosticTextRenderer = diagnosticTextRenderer;
            Id = Interlocked.Increment(ref s_nextId);
        }

        private FrameTimeGraph? CreateTimeGraph(string title)
        {
            if (_diagnosticTextRenderer == null)
                return null;
            return new FrameTimeGraph(360, new Size(360.0, 64.0), 1000.0 / 60.0, title, _diagnosticTextRenderer);
        }

        partial void OnIsEnabledChanged()
        {
            if (IsEnabled)
            {
                _compositor.AddCompositionTarget(this);
                foreach (var v in _attachedVisuals)
                    v.Activate();
            }
            else
            {
                _compositor.RemoveCompositionTarget(this);
                foreach (var v in _attachedVisuals)
                    v.Deactivate();
            }
        }

        partial void OnDebugOverlaysChanged()
        {
            if ((DebugOverlays & RendererDebugOverlays.Fps) == 0)
            {
                _fpsCounter?.Reset();
            }

            if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) == 0)
            {
                _layoutTimeGraph?.Reset();
            }

            if ((DebugOverlays & RendererDebugOverlays.RenderTimeGraph) == 0)
            {
                _renderTimeGraph?.Reset();
            }
        }

        partial void OnLastLayoutPassTimingChanged()
        {
            if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0)
            {
                LayoutTimeGraph?.AddFrameValue(LastLayoutPassTiming.Elapsed.TotalMilliseconds);
            }
        }

        partial void DeserializeChangesExtra(BatchStreamReader c)
        {
            _redrawRequested = true;
        }

        public void Render()
        {
            if (_disposed)
            {
                Compositor.RemoveCompositionTarget(this);
                return;
            }

            if (Root == null) 
                return;

            if (_renderTarget?.IsCorrupted == true)
            {
                _layer?.Dispose();
                _layer = null;
                _renderTarget.Dispose();
                _renderTarget = null;
                _redrawRequested = true;
            }

            try
            {
                _renderTarget ??= _compositor.CreateRenderTarget(_surfaces());
            }
            catch (RenderTargetNotReadyException)
            {
                return;
            }
            catch (RenderTargetCorruptedException)
            {
                return;
            }

            if ((_dirtyRect.Width == 0 && _dirtyRect.Height == 0) && !_redrawRequested)
                return;

            Revision++;

            var captureTiming = (DebugOverlays & RendererDebugOverlays.RenderTimeGraph) != 0;
            var startingTimestamp = captureTiming ? Stopwatch.GetTimestamp() : 0L;

            // Update happens in a separate phase to extend dirty rect if needed
            Root.Update(this);

            while (_adornerUpdateQueue.Count > 0)
            {
                var adorner = _adornerUpdateQueue.Dequeue();
                adorner.Update(this);
            }
            
            Readback.CompleteWrite(Revision);

            _redrawRequested = false;
            using (var targetContext = _renderTarget.CreateDrawingContext())
            {
                var size = Size;
                var layerSize = size * Scaling;
                if (layerSize != _layerSize || _layer == null || _layer.IsCorrupted)
                {
                    _layer?.Dispose();
                    _layer = null;
                    _layer = targetContext.CreateLayer(size);
                    _layerSize = layerSize;
                    _dirtyRect = new Rect(0, 0, size.Width, size.Height);
                }

                if (_dirtyRect.Width != 0 || _dirtyRect.Height != 0)
                {
                    using (var context = _layer.CreateDrawingContext())
                    {
                        context.PushClip(_dirtyRect);
                        context.Clear(Colors.Transparent);
                        Root.Render(new CompositorDrawingContextProxy(context), _dirtyRect);
                        context.PopClip();
                    }
                }

                targetContext.Clear(Colors.Transparent);
                targetContext.Transform = Matrix.Identity;
                if (_layer.CanBlit)
                    _layer.Blit(targetContext);
                else
                    targetContext.DrawBitmap(_layer, 1,
                        new Rect(_layerSize),
                        new Rect(size));

                if (DebugOverlays != RendererDebugOverlays.None)
                {
                    if (captureTiming)
                    {
                        var elapsed = StopwatchHelper.GetElapsedTime(startingTimestamp);
                        RenderTimeGraph?.AddFrameValue(elapsed.TotalMilliseconds);
                    }

                    DrawOverlays(targetContext);
                }

                RenderedVisuals = 0;

                _dirtyRect = default;
            }
        }

        private void DrawOverlays(IDrawingContextImpl targetContext)
        {
            if ((DebugOverlays & RendererDebugOverlays.DirtyRects) != 0)
            {
                targetContext.DrawRectangle(
                    new ImmutableSolidColorBrush(
                        new Color(30, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
                    null,
                    _dirtyRect);
            }

            if ((DebugOverlays & RendererDebugOverlays.Fps) != 0)
            {
                var nativeMem = ByteSizeHelper.ToString((ulong) (
                    (Compositor.BatchMemoryPool.CurrentUsage + Compositor.BatchMemoryPool.CurrentPool) *
                    Compositor.BatchMemoryPool.BufferSize), false);
                var managedMem = ByteSizeHelper.ToString((ulong) (
                    (Compositor.BatchObjectPool.CurrentUsage + Compositor.BatchObjectPool.CurrentPool) *
                    Compositor.BatchObjectPool.ArraySize *
                    IntPtr.Size), false);
                FpsCounter?.RenderFps(targetContext,
                    FormattableString.Invariant($"M:{managedMem} / N:{nativeMem} R:{RenderedVisuals:0000}"));
            }

            var top = 0.0;

            void DrawTimeGraph(FrameTimeGraph? graph)
            {
                if (graph == null)
                    return;
                top += 8.0;
                targetContext.Transform = Matrix.CreateTranslation(Size.Width - graph.Size.Width - 8.0, top);
                graph.Render(targetContext);
                top += graph.Size.Height;
            }

            if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0)
            {
                DrawTimeGraph(LayoutTimeGraph);
            }

            if ((DebugOverlays & RendererDebugOverlays.RenderTimeGraph) != 0)
            {
                DrawTimeGraph(RenderTimeGraph);
            }
        }

        public Rect SnapToDevicePixels(Rect rect) => SnapToDevicePixels(rect, Scaling);
        
        private static Rect SnapToDevicePixels(Rect rect, double scale)
        {
            return new Rect(
                new Point(
                    Math.Floor(rect.X * scale) / scale,
                    Math.Floor(rect.Y * scale) / scale),
                new Point(
                    Math.Ceiling(rect.Right * scale) / scale,
                    Math.Ceiling(rect.Bottom * scale) / scale));
        }
        
        public void AddDirtyRect(Rect rect)
        {
            if (rect.Width == 0 && rect.Height == 0)
                return;
            var snapped = SnapToDevicePixels(rect, Scaling);
            DebugEvents?.RectInvalidated(rect);
            _dirtyRect = _dirtyRect.Union(snapped);
            _redrawRequested = true;
        }

        public void Invalidate()
        {
            _redrawRequested = true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            using (_compositor.RenderInterface.EnsureCurrent())
            {
                if (_layer != null)
                {
                    _layer.Dispose();
                    _layer = null;
                }

                _renderTarget?.Dispose();
                _renderTarget = null;
            }
            _compositor.RemoveCompositionTarget(this);
        }

        public void AddVisual(ServerCompositionVisual visual)
        {
            if (_attachedVisuals.Add(visual) && IsEnabled)
                visual.Activate();
        }

        public void RemoveVisual(ServerCompositionVisual visual)
        {
            if (_attachedVisuals.Remove(visual) && IsEnabled)
                visual.Deactivate();
            if (visual.IsVisibleInFrame)
                AddDirtyRect(visual.TransformedOwnContentBounds);
        }

        public void EnqueueAdornerUpdate(ServerCompositionVisual visual) => _adornerUpdateQueue.Enqueue(visual);
    }
}
