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
        private Batch _currentBatch;
        private bool _implicitBatchCommitQueued;
        private Action _implicitBatchCommit;

        internal Batch CurrentBatch => _currentBatch;
        internal ServerCompositor Server => _server;
        internal CompositionEasingFunction DefaultEasing { get; }

        private Compositor(ServerCompositor server)
        {
            _server = server;
            _currentBatch = new Batch();
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
            var batch = CurrentBatch;
            _currentBatch = new Batch();
            batch.CommitedAt = Server.Clock.Elapsed;
            _server.EnqueueBatch(batch);
            return batch.Completed;
        }

        public static Compositor Create(IRenderLoop timer)
        {
            return new Compositor(new ServerCompositor(timer));
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

        internal CustomDrawVisual<T> CreateCustomDrawVisual<T>(ICustomDrawVisualRenderer<T> renderer,
            ICustomDrawVisualHitTest<T>? hitTest = null) where T : IEquatable<T> =>
            new CustomDrawVisual<T>(this, renderer, hitTest);

        public void QueueImplicitBatchCommit()
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
    }
}