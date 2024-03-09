using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Collections.Pooled;
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
        private PixelSize _layerSize;
        private IDrawingContextLayerImpl? _layer;
        private bool _updateRequested;
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
            var platformRender = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            DirtyRects = compositor.Options.UseRegionDirtyRectClipping == true &&
                         platformRender?.SupportsRegions == true
                ? new RegionDirtyRectTracker(platformRender)
                : new DirtyRectTracker();
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

            if (DirtyRects.IsEmpty && !_redrawRequested && !_updateRequested)
                return;

            Revision++;

            var captureTiming = (DebugOverlays & RendererDebugOverlays.RenderTimeGraph) != 0;
            var startingTimestamp = captureTiming ? Stopwatch.GetTimestamp() : 0L;

            var transform = Matrix.CreateScale(Scaling, Scaling);
            // Update happens in a separate phase to extend dirty rect if needed
            Root.Update(this, transform);

            while (_adornerUpdateQueue.Count > 0)
            {
                var adorner = _adornerUpdateQueue.Dequeue();
                adorner.Update(this, transform);
            }

            _updateRequested = false;
            Readback.CompleteWrite(Revision);

            if (!_redrawRequested)
                return;
            _redrawRequested = false;
            using (var targetContext = _renderTarget.CreateDrawingContext(false))
            {
                if (PixelSize != _layerSize || _layer == null || _layer.IsCorrupted)
                {
                    _layer?.Dispose();
                    _layer = null;
                    _layer = targetContext.CreateLayer(PixelSize);
                    _layerSize = PixelSize;
                    DirtyRects.AddRect(new PixelRect(_layerSize));
                }

                if (!DirtyRects.IsEmpty)
                {
                    var useLayerClip = Compositor.Options.UseSaveLayerRootClip ??
                                       Compositor.RenderInterface.GpuContext != null;
                    using (var context = _layer.CreateDrawingContext(false))
                    {
                        using (DirtyRects.BeginDraw(context))
                        {
                            context.Clear(Colors.Transparent);
                            if (useLayerClip) 
                                context.PushLayer(DirtyRects.CombinedRect.ToRect(1));
                                
                            
                            Root.Render(new CompositorDrawingContextProxy(context), null, DirtyRects);

                            if (useLayerClip)
                                context.PopLayer();
                        }
                    }
                }

                targetContext.Clear(Colors.Transparent);
                targetContext.Transform = Matrix.Identity;
                if (_layer.CanBlit)
                    _layer.Blit(targetContext);
                else
                {
                    var rect = new PixelRect(default, PixelSize).ToRect(1);
                    targetContext.DrawBitmap(_layer, 1, rect, rect);
                }

                if (DebugOverlays != RendererDebugOverlays.None)
                {
                    if (captureTiming)
                    {
                        var elapsed = StopwatchHelper.GetElapsedTime(startingTimestamp);
                        RenderTimeGraph?.AddFrameValue(elapsed.TotalMilliseconds);
                    }
                    
                    DrawOverlays(targetContext, PixelSize.ToSize(Scaling));
                }

                RenderedVisuals = 0;

                DirtyRects.Reset();
            }
        }

        private void DrawOverlays(IDrawingContextImpl targetContext, Size logicalSize)
        {
            if ((DebugOverlays & RendererDebugOverlays.DirtyRects) != 0) 
                DirtyRects.Visualize(targetContext);

            
            targetContext.Transform = Matrix.CreateScale(Scaling, Scaling);
            
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
                var oldTransform = targetContext.Transform;

                targetContext.Transform = Matrix.CreateTranslation(logicalSize.Width - graph.Size.Width - 8.0, top) *
                                          oldTransform;
                graph.Render(targetContext);
                top += graph.Size.Height;
                targetContext.Transform = oldTransform;
            }

            if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0)
            {
                DrawTimeGraph(LayoutTimeGraph);
            }

            if ((DebugOverlays & RendererDebugOverlays.RenderTimeGraph) != 0)
            {
                DrawTimeGraph(RenderTimeGraph);
            }
            
            targetContext.Transform = Matrix.Identity;
        }
        
        public void RequestUpdate() => _updateRequested = true;

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
