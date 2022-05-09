using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Animations
{
    internal struct AnimatedValueStore<T> where T : struct
    {
        private T _direct;
        private IAnimationInstance _animation;
        private T? _lastAnimated;

        public T Direct => _direct;

        public T GetAnimated(ServerCompositor compositor)
        {
            if (_animation == null)
                return _direct;
            var v = _animation.Evaluate(compositor.ServerNow, ExpressionVariant.Create(_direct))
                .CastOrDefault<T>();
            _lastAnimated = v;
            return v;
        }

        private T LastAnimated => _animation != null ? _lastAnimated ?? _direct : _direct;

        public bool IsAnimation => _animation != null;

        public void SetAnimation(ChangeSet cs, IAnimationInstance animation)
        {
            _animation = animation;
            _animation.Start(cs.Batch.CommitedAt, ExpressionVariant.Create(LastAnimated));
        }

        public static implicit operator AnimatedValueStore<T>(T value) => new AnimatedValueStore<T>()
        {
            _direct = value
        };
    }
}