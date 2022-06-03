using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionTarget : IDisposable
    {
        private readonly ServerCompositor _compositor;
        private readonly Func<IRenderTarget> _renderTargetFactory;
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


        public ReadbackIndices Readback { get; } = new();

        public ServerCompositionTarget(ServerCompositor compositor, Func<IRenderTarget> renderTargetFactory) :
            base(compositor)
        {
            _compositor = compositor;
            _renderTargetFactory = renderTargetFactory;
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
            _renderTarget ??= _renderTargetFactory();

            Compositor.UpdateServerTime();
            
            if(_dirtyRect.IsEmpty && !_redrawRequested)
                return;

            Revision++;
            
            // Update happens in a separate phase to extend dirty rect if needed
            Root.Update(this, Matrix4x4.Identity);

            while (_adornerUpdateQueue.Count > 0)
            {
                var adorner = _adornerUpdateQueue.Dequeue();
                adorner.Update(this, adorner.AdornedVisual?.GlobalTransformMatrix ?? Matrix4x4.Identity);
            }
            
            Readback.CompleteWrite(Revision);

            _redrawRequested = false;
            using (var targetContext = _renderTarget.CreateDrawingContext(null))
            {
                var layerSize = Size * Scaling;
                if (layerSize != _layerSize || _layer == null)
                {
                    _layer?.Dispose();
                    _layer = null;
                    _layer = targetContext.CreateLayer(layerSize);
                    _layerSize = layerSize;
                }

                if (!_dirtyRect.IsEmpty)
                {
                    var visualBrushHelper = new CompositorDrawingContextProxy.VisualBrushRenderer();
                    using (var context = _layer.CreateDrawingContext(visualBrushHelper))
                    {
                        context.PushClip(_dirtyRect);
                        context.Clear(Colors.Transparent);
                        Root.Render(new CompositorDrawingContextProxy(context, visualBrushHelper));
                        context.PopClip();
                    }
                }

                targetContext.DrawBitmap(RefCountable.CreateUnownedNotClonable(_layer), 1, new Rect(_layerSize),
                    new Rect(_layerSize));
                
                
                if (DrawDirtyRects)
                {
                    targetContext.DrawRectangle(new ImmutableSolidColorBrush(
                            new Color(30, (byte)_random.Next(255), (byte)_random.Next(255),
                                (byte)_random.Next(255)))
                        , null, _dirtyRect);
                }

                if(DrawFps)
                    _fpsCounter.RenderFps(targetContext);
                _dirtyRect = Rect.Empty;
                
            }
        }

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
            var snapped = SnapToDevicePixels(rect, Scaling);
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
            using (_compositor.GpuContext?.EnsureCurrent())
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
                AddDirtyRect(visual.TransformedBounds);
        }

        public void EnqueueAdornerUpdate(ServerCompositionVisual visual) => _adornerUpdateQueue.Enqueue(visual);
    }
}