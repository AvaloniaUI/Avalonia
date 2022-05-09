using System;
using System.Numerics;
using System.Threading;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionTarget
    {
        private readonly ServerCompositor _compositor;
        private readonly Func<IRenderTarget> _renderTargetFactory;
        private static long s_nextId = 1;
        public long Id { get; }
        private ulong _frame = 1;
        private IRenderTarget? _renderTarget;

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
                _compositor.AddCompositionTarget(this);
            else
                _compositor.RemoveCompositionTarget(this);
        }

        public void Render()
        {
            if (Root == null) 
                return;
            _renderTarget ??= _renderTargetFactory();

            Compositor.UpdateServerTime();
            using (var context = _renderTarget.CreateDrawingContext(null))
            {
                context.Clear(Colors.Transparent);
                Root.Render(new CompositorDrawingContextProxy(context), Root.CombinedTransformMatrix);
            }

            Readback.NextWrite(_frame);
            _frame++;
        }
    }
}