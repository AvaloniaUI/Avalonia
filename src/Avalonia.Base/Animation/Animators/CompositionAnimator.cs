using System;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation.Animators
{
    internal abstract class CompositionAnimator<T> : AvaloniaList<AnimatorKeyFrame>, IAnimator
    {
        public AvaloniaProperty? Property { get; set; }

        protected abstract KeyFrameAnimation? GetAnimation(Visual visual);

        public IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete, bool shouldPauseOnInvisible)
        {
            if (control is not Visual visual)
                throw new InvalidOperationException("Composition animations can only be applied to Visuals.");

            var subject = new DisposeCompositionAnimationInstanceSubject<T>(this,
                animation,
                visual);

            return new CompositeDisposable(match.Subscribe(subject), subject);
        }

        protected abstract void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easings.Easing easing);

        public IDisposable AttachAnimation(Visual visual, Animation animation)
        {
            var property = Property ?? throw new InvalidOperationException("Property must be set before attaching animation.");

            if (property.OwnerType != typeof(Composition))
                throw new ArgumentException
                    ($"Invalid Target property \"{property.Name}\" for this animation: {GetType().Name}. Target must be a Composition property.");

            var compositionAnimation = GetAnimation(visual);

            if (compositionAnimation is null)
                throw new InvalidOperationException("Composition animation cannot be null.");

            var compositionVisual = ElementComposition.GetElementVisual(visual);

            if (compositionVisual is null)
                throw new InvalidOperationException("Visual is not attached");

            compositionAnimation.Duration = animation.Duration;
            compositionAnimation.DelayTime = animation.Delay;
            compositionAnimation.IterationCount = (int)animation.IterationCount.Value;
            compositionAnimation.IterationBehavior = animation.IterationCount.IsInfinite ? AnimationIterationBehavior.Forever : AnimationIterationBehavior.Count;
            compositionAnimation.StopBehavior = animation.FillMode switch
            {
                FillMode.Forward => AnimationStopBehavior.SetToFinalValue,
                FillMode.Backward => AnimationStopBehavior.SetToInitialValue,
                _ => AnimationStopBehavior.LeaveCurrentValue,
            };

            var target = property.Name;

            foreach (var keyframe in this)
            {
                var easing = keyframe.KeySpline is not null
                    ? new SplineEasing(keyframe.KeySpline.ControlPointX1, keyframe.KeySpline.ControlPointX2, keyframe.KeySpline.ControlPointY1, keyframe.KeySpline.ControlPointY2)
                    : animation.Easing;
                SetKeyFrame(compositionAnimation, keyframe, easing);
            }

            compositionVisual.StartAnimation(target, compositionAnimation);

            return Disposable.Create(() => compositionVisual.StopAnimation(target));
        }
    }
}
