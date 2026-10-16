using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Collections.Pooled;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
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
        private readonly Func<IEnumerable<IPlatformRenderSurface>> _surfaces;
        private CompositionTargetOverlays _overlays;
        private static long s_nextId = 1;
        private IRenderTarget? _renderTarget;
        private PixelSize _layerSize;
        private IDrawingContextLayerImpl? _layer;
        private PartialPresentationDirtyRectHistory? _partialPresentationHistory;
        private Matrix _outputTransform = Matrix.Identity;
        private bool _updateRequested;
        private bool _redrawRequested;
        private bool _fullRedrawRequested;
        private bool _disposed;
        private readonly HashSet<ServerCompositionVisual> _attachedVisuals = new();
        public IDirtyRectTracker DirtyRects { get; }

        public long Id { get; }
        public ulong Revision { get; private set; }
        public ICompositionTargetDebugEvents? DebugEvents { get; set; }
        public int RenderedVisuals { get; set; }
        public int VisitedVisuals { get; set; }

        internal PixelSize PixelSize => Avalonia.PixelSize.FromSizeCeiling(Size, Scaling);
        
        /// <summary>
        /// Returns true if the target is enabled and has pending work but its render target was not ready.
        /// </summary>
        internal bool IsWaitingForReadyRenderTarget { get; private set; }
        
        /// <summary>
        /// Returns true if the target's render target is waiting for a render loop wakeup
        /// (i.e. the platform will call Wakeup() when ready, no need to keep polling).
        /// </summary>
        internal bool IsWaitingForRenderLoopWakeup { get; private set; }

        public ServerCompositionTarget(ServerCompositor compositor, Func<IEnumerable<IPlatformRenderSurface>> surfaces)
            : base(compositor)
        {
            _compositor = compositor;
            _surfaces = surfaces;
            _overlays = new CompositionTargetOverlays(this);
            var platformRender = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            if (platformRender?.SupportsRegions == true && compositor.Options.UseRegionDirtyRectClipping == true)
            {
                var maxRects = compositor.Options.MaxDirtyRects ?? 8;
                DirtyRects = maxRects <= 0
                    ? new RegionDirtyRectTracker(platformRender)
                    : new MultiDirtyRectTracker(platformRender, maxRects,
                        // WPF uses 50K, but that merges stuff rather aggressively 
                        compositor.Options.DirtyRectMergeEagerness ?? 1000); 
            }

            DirtyRects ??= new SingleDirtyRectTracker();
            
            Id = Interlocked.Increment(ref s_nextId);
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
            _fullRedrawRequested = true;
            _partialPresentationHistory = null;
            _overlays.OnChanged(DebugOverlays);
        }

        partial void OnLastLayoutPassTimingChanged() => _overlays.OnLastLayoutPassTimingChanged(LastLayoutPassTiming);

        partial void DeserializeChangesExtra(BatchStreamReader c)
        {
            _redrawRequested = true;
            _fullRedrawRequested = true;
        }
        
        
        public void Update(TimeSpan diagnosticsCompositorGlobalUpdateElapsedTime = default)
        {
            if (_disposed)
            {
                Compositor.RemoveCompositionTarget(this);
                return;
            }

            if (Root == null)
                return;
            
            _overlays.RecordGlobalCompositorUpdateTime(diagnosticsCompositorGlobalUpdateElapsedTime);
            _overlays.MarkUpdateCallStart();
            using (Diagnostic.BeginCompositorUpdatePass())
            {
                var transform = Matrix.CreateScale(Scaling, Scaling);

                var collector = DebugEvents != null
                    ? new DebugEventsDirtyRectCollectorProxy(DirtyRects, DebugEvents)
                    : (IDirtyRectCollector)DirtyRects;
                
                Root.UpdateRoot(collector, transform, new LtrbRect(0, 0, PixelSize.Width, PixelSize.Height));

                _updateRequested = false;

                _overlays.MarkUpdateCallEnd();
            }
        }

        public void Render()
        {
            IsWaitingForReadyRenderTarget = false;
            IsWaitingForRenderLoopWakeup = false;
            
            if (_disposed)
                return;

            if (Root == null) 
                return;

            if (_renderTarget?.PlatformRenderTargetState.IsCorrupted == true)
            {
                _layer?.Dispose();
                _layer = null;
                _renderTarget.Dispose();
                _renderTarget = null;
                _redrawRequested = true;
                _fullRedrawRequested = true;
                _partialPresentationHistory = null;
            }

            try
            {
                if (_renderTarget == null)
                {
                    if (!_compositor.IsReadyToCreateRenderTarget(_surfaces()))
                    {
                        IsWaitingForReadyRenderTarget = IsEnabled;
                        return;
                    }

                    _renderTarget = _compositor.CreateRenderTarget(_surfaces());
                }
            }
            catch (RenderTargetNotReadyException)
            {
                IsWaitingForReadyRenderTarget = IsEnabled;
                return;
            }
            catch (RenderTargetCorruptedException)
            {
                return;
            }

            if (DirtyRects.IsEmpty && !_redrawRequested && !_updateRequested)
                return;

            _redrawRequested |= !DirtyRects.IsEmpty;

            if (!_redrawRequested)
                return;
            
            if (!_renderTarget.PlatformRenderTargetState.IsReady)
            {
                IsWaitingForReadyRenderTarget = IsEnabled;
                IsWaitingForRenderLoopWakeup = IsEnabled && _renderTarget.PlatformRenderTargetState.WillWakeUpRenderLoopWhenReady;
                return;
            }

            var targetProperties = _renderTarget.Properties;
            // Exact quarter turns keep pixel edges aligned, including partial presentation clips.
            var outputTransform = targetProperties.RequiresRotationForOrientation switch
            {
                SurfaceOrientation.Rotation90 => new Matrix(0, 1, -1, 0, PixelSize.Height, 0),
                SurfaceOrientation.Rotation180 => new Matrix(-1, 0, 0, -1, PixelSize.Width, PixelSize.Height),
                SurfaceOrientation.Rotation270 => new Matrix(0, -1, 1, 0, 0, PixelSize.Width),
                _ => Matrix.Identity
            };
            if (_outputTransform != outputTransform)
            {
                _outputTransform = outputTransform;
                _partialPresentationHistory = null;
                _fullRedrawRequested = true;
            }
            var usePartialPresentation = targetProperties.SupportsPartialPresentation
                                         && DebugOverlays == RendererDebugOverlays.None;
            var needLayer = !outputTransform.IsIdentity || _overlays.RequireLayer
                            || !(targetProperties.IsSuitableForDirectRendering
                                 && (targetProperties.RetainsPreviousFrameContents
                                     || usePartialPresentation));

            IDrawingContextImpl renderTargetContext;
            RenderTargetDrawingContextProperties properties;
            try
            {
                renderTargetContext =
                    _renderTarget.CreateDrawingContext(
                        new(PixelSize, Scaling, Size, TransparencyLevel, PlatformSpecificSceneInfo),
                        out properties);
            }
            catch (RenderTargetNotReadyException)
            {
                IsWaitingForReadyRenderTarget = IsEnabled;
                return;
            }
            catch (RenderTargetCorruptedException)
            {
                return;
            }
            
            var renderBounds = new LtrbRect(0, 0, PixelSize.Width, PixelSize.Height);
            IPartialPresentationFeature? partialPresentation = null;
            PartialPresentationDirtyRectHistory? partialPresentationHistory = null;
            try
            {
                using (renderTargetContext)
                {
                    using (var renderTiming = Diagnostic.BeginCompositorRenderPass())
                    {
                        var recreateLayer = needLayer &&
                                            (PixelSize != _layerSize || _layer == null || _layer.IsCorrupted);
                        if (_fullRedrawRequested || recreateLayer
                                                 || (!needLayer && !usePartialPresentation &&
                                                     !properties.PreviousFrameIsRetained))
                        {
                            DirtyRects.Initialize(renderBounds);
                            DirtyRects.AddRect(renderBounds);
                        }

                        DirtyRects.FinalizeFrame(renderBounds);

                        // Overlay frames still need session completion, but use full presentation.
                        if (targetProperties.SupportsPartialPresentation)
                        {
                            partialPresentation = renderTargetContext.GetFeature<IPartialPresentationFeature>()
                                                  ?? throw new InvalidOperationException(
                                                      "The render target did not provide its partial presentation feature after announcing it.");

                            if (usePartialPresentation)
                            {
                                partialPresentationHistory = _partialPresentationHistory ??=
                                    new PartialPresentationDirtyRectHistory();
                                partialPresentationHistory.BeforeDraw(partialPresentation, DirtyRects);
                            }
                        }

                        if (recreateLayer)
                        {
                            _layer?.Dispose();
                            _layer = null;
                            _layer = renderTargetContext.CreateLayer(PixelSize);
                            _layerSize = PixelSize;
                        }
                        else if (!needLayer)
                        {
                            _layer?.Dispose();
                            _layer = null;
                        }

                        // For retained layer we are rendering before taking partial presentation into consideration
                        // since it's only concerned about _current_ frame's content
                        if (_layer != null && !DirtyRects.IsEmpty)
                            using (var context = _layer.CreateDrawingContext())
                                RenderRootToContextWithClip(context, Root);

                        // Replay dirty rect history and declare the rectangles before drawing to the output buffer.
                        partialPresentationHistory?.BeforeTargetDraw(DirtyRects, renderBounds, outputTransform);

                        if (_layer != null) // Intermediate buffer rendering path
                        {
                            renderTargetContext.Transform = Matrix.Identity;
                            var rect = new PixelRect(PixelSize).ToRect(1);
                            if (partialPresentationHistory != null)
                            {
                                // BufferDirtyRects are already merged by DirtyRects; the default GPU tracker
                                // produces one rectangle. Multiple rectangles require opting into region clipping.
                                // Native layer blits can ignore the canvas clip and clear the whole output.
                                // Rectangular bitmap copies also work on framebuffers without stencil.
                                foreach (var dirtyRect in partialPresentationHistory.BufferDirtyRects)
                                {
                                    renderTargetContext.Transform = Matrix.Identity;
                                    renderTargetContext.PushClip(dirtyRect.ToRect(1));
                                    try
                                    {
                                        renderTargetContext.Clear(Colors.Transparent);
                                        renderTargetContext.Transform = outputTransform;
                                        renderTargetContext.DrawBitmap(_layer, 1, rect, rect);
                                    }
                                    finally
                                    {
                                        renderTargetContext.PopClip();
                                    }
                                }
                            }
                            else
                            {
                                renderTargetContext.Clear(Colors.Transparent);
                                renderTargetContext.Transform = outputTransform;
                                if (_layer.CanBlit && outputTransform.IsIdentity)
                                    _layer.Blit(renderTargetContext);
                                else
                                    renderTargetContext.DrawBitmap(_layer, 1, rect, rect);
                            }
                            _overlays.Draw(renderTargetContext, true, outputTransform);
                        }
                        else // Direct rendering path
                        {
                            if (!DirtyRects.IsEmpty)
                            {
                                RenderRootToContextWithClip(renderTargetContext, Root);
                                _overlays.Draw(renderTargetContext, false);
                            }
                        }
                    }

                    // Report that the frame we've rendered is actually not garbage,
                    // will be important when we ask for a drawing context next time
                    // Not doing this may also skip presentation entirely
                    partialPresentation?.Succeeded();
                }

                // Record frame into history
                partialPresentationHistory?.AfterSuccessfulDraw();
                // Reset state
                _fullRedrawRequested = false;
                _redrawRequested = false;
                DirtyRects.Initialize(renderBounds);

            }
            catch
            {
                _fullRedrawRequested = true;
                _redrawRequested = true;
                _partialPresentationHistory = null;
                throw;
            }
            finally
            {
                RenderedVisuals = 0;
                VisitedVisuals = 0;
            }
        }

        void RenderRootToContextWithClip(IDrawingContextImpl context, ServerCompositionVisual root)
        {
            var useLayerClip = Compositor.Options.UseSaveLayerRootClip ?? false;

            using (DirtyRects.BeginDraw(context))
            {
                context.Clear(Colors.Transparent);
                if (useLayerClip)
                    context.PushLayer(DirtyRects.CombinedRect.ToRect());

                context.Transform = Matrix.CreateScale(Scaling, Scaling);
                (VisitedVisuals, RenderedVisuals) = root.Render(context, new LtrbRect(0,0, PixelSize.Width, PixelSize.Height), DirtyRects);
                if (DebugEvents != null)
                {
                    DebugEvents.RenderedVisuals = RenderedVisuals;
                    DebugEvents.VisitedVisuals = VisitedVisuals;
                }

                if (useLayerClip)
                    context.PopLayer();
            }
        }

        public void RequestUpdate() => _updateRequested = true;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ResetRenderTarget();
            _compositor.RemoveCompositionTarget(this);
        }

        public void ResetRenderTarget()
        {
            _partialPresentationHistory = null;
            _fullRedrawRequested = true;
            if (_layer == null && _renderTarget == null)
                return;
            try
            {
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
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Unable to make the render interface current: {Error}", ex);
                // Set to null for now
                // TODO: Check per-platform to make sure that it's safe to dispose anyay
                _layer = null;
                _renderTarget = null;
                
            }

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
        }

        public void RequestFullRedraw() => _redrawRequested = true;
    }
}
