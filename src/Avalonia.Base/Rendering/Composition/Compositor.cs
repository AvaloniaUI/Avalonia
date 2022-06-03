using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;


namespace Avalonia.Rendering.Composition
{
    public partial class Compositor
    {
        private ServerCompositor _server;
        private bool _implicitBatchCommitQueued;
        private Action _implicitBatchCommit;
        private BatchStreamObjectPool<object?> _batchObjectPool = new();
        private BatchStreamMemoryPool _batchMemoryPool = new();
        private List<CompositionObject> _objectsForSerialization = new();
        internal ServerCompositor Server => _server;
        internal CompositionEasingFunction DefaultEasing { get; }
        
        public Compositor(IRenderLoop loop, IPlatformGpu? gpu)
        {
            _server = new ServerCompositor(loop, gpu, _batchObjectPool, _batchMemoryPool);
            _implicitBatchCommit = ImplicitBatchCommit;
            DefaultEasing = new CubicBezierEasingFunction(this,
                new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));
        }

        public CompositionTarget CreateCompositionTarget(Func<IRenderTarget> renderTargetFactory)
        {
            return new CompositionTarget(this, new ServerCompositionTarget(_server, renderTargetFactory));
        }

        public Task RequestCommitAsync()
        {
            var batch = new Batch();
            
            using (var writer = new BatchStreamWriter(batch.Changes, _batchMemoryPool, _batchObjectPool))
            {
                foreach (var obj in _objectsForSerialization)
                {
                    writer.WriteObject(obj.Server);
                    obj.SerializeChanges(writer);
#if DEBUG_COMPOSITOR_SERIALIZATION
                    writer.Write(BatchStreamDebugMarkers.ObjectEndMagic);
                    writer.WriteObject(BatchStreamDebugMarkers.ObjectEndMarker);
#endif
                }
                _objectsForSerialization.Clear();
            }
            
            batch.CommitedAt = Server.Clock.Elapsed;
            _server.EnqueueBatch(batch);
            return batch.Completed;
        }

        public void Dispose()
        {
            
        }

        public CompositionContainerVisual CreateContainerVisual() => new(this, new ServerCompositionContainerVisual(_server));
        
        public CompositionSolidColorVisual CreateSolidColorVisual() => new CompositionSolidColorVisual(this, 
            new ServerCompositionSolidColorVisual(_server));

        public CompositionSolidColorVisual CreateSolidColorVisual(Avalonia.Media.Color color)
        {
            var v = new CompositionSolidColorVisual(this, new ServerCompositionSolidColorVisual(_server));
            v.Color = color;
            return v;
        }

        public CompositionSpriteVisual CreateSpriteVisual() => new CompositionSpriteVisual(this, new ServerCompositionSpriteVisual(_server));

        public CompositionLinearGradientBrush CreateLinearGradientBrush() 
            => new CompositionLinearGradientBrush(this, new ServerCompositionLinearGradientBrush(_server));

        public CompositionColorGradientStop CreateColorGradientStop()
            => new CompositionColorGradientStop(this, new ServerCompositionColorGradientStop(_server));

        public CompositionColorGradientStop CreateColorGradientStop(float offset, Avalonia.Media.Color color)
        {
            var stop = CreateColorGradientStop();
            stop.Offset = offset;
            stop.Color = color;
            return stop;
        }

        // We want to make it 100% async later
        /*
        public CompositionBitmapSurface LoadBitmapSurface(Stream stream)
        {
            var bmp = _server.Backend.LoadCpuMemoryBitmap(stream);
            return new CompositionBitmapSurface(this, bmp);
        }

        public async Task<CompositionBitmapSurface> LoadBitmapSurfaceAsync(Stream stream)
        {
            var bmp = await Task.Run(() => _server.Backend.LoadCpuMemoryBitmap(stream));
            return new CompositionBitmapSurface(this, bmp);
        }
        */
        public CompositionColorBrush CreateColorBrush(Avalonia.Media.Color color) =>
            new CompositionColorBrush(this, new ServerCompositionColorBrush(_server)) {Color = color};

        public CompositionSurfaceBrush CreateSurfaceBrush() =>
            new CompositionSurfaceBrush(this, new ServerCompositionSurfaceBrush(_server));

        /*
        public CompositionGaussianBlurEffectBrush CreateGaussianBlurEffectBrush() =>
            new CompositionGaussianBlurEffectBrush(this, new ServerCompositionGaussianBlurEffectBrush(_server));

        public CompositionBackdropBrush CreateBackdropBrush() =>
            new CompositionBackdropBrush(this, new ServerCompositionBackdropBrush(Server));*/

        public ExpressionAnimation CreateExpressionAnimation() => new ExpressionAnimation(this);

        public ExpressionAnimation CreateExpressionAnimation(string expression) => new ExpressionAnimation(this)
        {
            Expression = expression
        };

        public ImplicitAnimationCollection CreateImplicitAnimationCollection() => new ImplicitAnimationCollection(this);

        public CompositionAnimationGroup CreateAnimationGroup() => new CompositionAnimationGroup(this);
        
        private void QueueImplicitBatchCommit()
        {
            if(_implicitBatchCommitQueued)
                return;
            _implicitBatchCommitQueued = true;
            Dispatcher.UIThread.Post(_implicitBatchCommit, DispatcherPriority.CompositionBatch);
        }

        private void ImplicitBatchCommit()
        {
            _implicitBatchCommitQueued = false;
            RequestCommitAsync();
        }

        internal void RegisterForSerialization(CompositionObject compositionObject)
        {
            _objectsForSerialization.Add(compositionObject);
            QueueImplicitBatchCommit();
        }
    }
}