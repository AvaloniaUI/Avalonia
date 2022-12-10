using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

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
        private static long s_nextId = 1;
        public long Id { get; }
        public ulong Revision { get; private set; }
        private IRenderTarget? _renderTarget;
        private FpsCounter _fpsCounter = new FpsCounter(Typeface.Default.GlyphTypeface);
        private Rect _dirtyRect;
        private Random _random = new();
        private Size _layerSize;
        private IDrawingContextLayerImpl? _layer;
        private bool _redrawRequested;
        private bool _disposed;
        private HashSet<ServerCompositionVisual> _attachedVisuals = new();
        private Queue<ServerCompositionVisual> _adornerUpdateQueue = new();

        public ICompositionTargetDebugEvents? DebugEvents { get; set; }
        public ReadbackIndices Readback { get; } = new();
        public int RenderedVisuals { get; set; }

        public ServerCompositionTarget(ServerCompositor compositor, Func<IEnumerable<object>> surfaces) :
            base(compositor)
        {
            _compositor = compositor;
            _surfaces = surfaces;
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
                _renderTarget!.Dispose();
                _renderTarget = null;
                _redrawRequested = true;
            }

            _renderTarget ??= _compositor.CreateRenderTarget(_surfaces());

            Compositor.UpdateServerTime();
            
            if(_dirtyRect.IsEmpty && !_redrawRequested)
                return;

            Revision++;
            
            // Update happens in a separate phase to extend dirty rect if needed
            Root.Update(this);

            while (_adornerUpdateQueue.Count > 0)
            {
                var adorner = _adornerUpdateQueue.Dequeue();
                adorner.Update(this);
            }
            
            Readback.CompleteWrite(Revision);

            _redrawRequested = false;
            using (var targetContext = _renderTarget.CreateDrawingContext(null))
            {
                var layerSize = Size * Scaling;
                if (layerSize != _layerSize || _layer == null || _layer.IsCorrupted)
                {
                    _layer?.Dispose();
                    _layer = null;
                    _layer = targetContext.CreateLayer(Size);
                    _layerSize = layerSize;
                    _dirtyRect = new Rect(0, 0, layerSize.Width, layerSize.Height);
                }

                if (!_dirtyRect.IsEmpty)
                {
                    var visualBrushHelper = new CompositorDrawingContextProxy.VisualBrushRenderer();
                    using (var context = _layer.CreateDrawingContext(visualBrushHelper))
                    {
                        context.PushClip(_dirtyRect);
                        context.Clear(Colors.Transparent);
                        Root.Render(new CompositorDrawingContextProxy(context, visualBrushHelper), _dirtyRect);
                        context.PopClip();
                    }
                }

                targetContext.Clear(Colors.Transparent);
                targetContext.Transform = Matrix.Identity;
                if (_layer.CanBlit)
                    _layer.Blit(targetContext);
                else
                    targetContext.DrawBitmap(RefCountable.CreateUnownedNotClonable(_layer), 1,
                        new Rect(_layerSize),
                        new Rect(Size), BitmapInterpolationMode.LowQuality);
                
                
                if (DrawDirtyRects)
                {
                    targetContext.DrawRectangle(new ImmutableSolidColorBrush(
                            new Color(30, (byte)_random.Next(255), (byte)_random.Next(255),
                                (byte)_random.Next(255)))
                        , null, _dirtyRect);
                }

                if (DrawFps)
                {
                    var nativeMem = ByteSizeHelper.ToString((ulong)(
                        (Compositor.BatchMemoryPool.CurrentUsage + Compositor.BatchMemoryPool.CurrentPool)  *
                                                    Compositor.BatchMemoryPool.BufferSize), false);
                    var managedMem = ByteSizeHelper.ToString((ulong)(
                        (Compositor.BatchObjectPool.CurrentUsage + Compositor.BatchObjectPool.CurrentPool) *
                                                                     Compositor.BatchObjectPool.ArraySize *
                                                                     IntPtr.Size), false);
                    _fpsCounter.RenderFps(targetContext, FormattableString.Invariant($"M:{managedMem} / N:{nativeMem} R:{RenderedVisuals:0000}"));
                }
                RenderedVisuals = 0;

                _dirtyRect = Rect.Empty;
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
            if(rect.IsEmpty)
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
            if(_disposed)
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
            if(visual.IsVisibleInFrame)
                AddDirtyRect(visual.TransformedOwnContentBounds);
        }

        public void EnqueueAdornerUpdate(ServerCompositionVisual visual) => _adornerUpdateQueue.Enqueue(visual);
    }
}
