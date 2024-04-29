using System;
using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Base class for <see cref="Animator{T}"/> objects
    /// </summary>
    internal abstract class Animator<T> : AvaloniaList<AnimatorKeyFrame>, IAnimator
    {
        /// <summary>
        /// Gets or sets the target property for the keyframe.
        /// </summary>
        public AvaloniaProperty? Property { get; set; }

        /// <inheritdoc/>
        public virtual IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete)
        {
            var subject = new DisposeAnimationInstanceSubject<T>(this, animation, control, clock, onComplete);
            return new CompositeDisposable(match.Subscribe(subject), subject);
        }

        protected T InterpolationHandler(double animationTime, T neutralValue)
        {
            if (Count == 0)
                return neutralValue;

            var (beforeKeyFrame, afterKeyFrame) = FindKeyFrames(animationTime);

            double beforeTime, afterTime;
            T beforeValue, afterValue;

            if (beforeKeyFrame is null)
            {
                beforeTime = 0.0;
                beforeValue = afterKeyFrame is { FillBefore: true, Value: T fillValue } ? fillValue : neutralValue;
            }
            else
            {
                beforeTime = beforeKeyFrame.Cue.CueValue;
                beforeValue = beforeKeyFrame.Value is T value ? value : neutralValue;
            }

            if (afterKeyFrame is null)
            {
                afterTime = 1.0;
                afterValue = beforeKeyFrame is { FillAfter: true, Value: T fillValue } ? fillValue : neutralValue;
            }
            else
            {
                afterTime = afterKeyFrame.Cue.CueValue;
                afterValue = afterKeyFrame.Value is T value ? value : neutralValue;
            }

            var progress = (animationTime - beforeTime) / (afterTime - beforeTime);

            if (afterKeyFrame?.KeySpline is { } keySpline)
                progress = keySpline.GetSplineProgress(progress);

            return Interpolate(progress, beforeValue, afterValue);
        }

        private (AnimatorKeyFrame? Before, AnimatorKeyFrame? After) FindKeyFrames(double time)
        {
            Debug.Assert(Count >= 1);

            for (var i = 0; i < Count; i++)
            {
                var keyFrame = this[i];
                var keyFrameTime = keyFrame.Cue.CueValue;

                if (time < keyFrameTime || keyFrameTime == 1.0)
                    return (i > 0 ? this[i - 1] : null, keyFrame);
            }

            return (this[Count - 1], null);
        }

        public virtual IDisposable BindAnimation(Animatable control, IObservable<T> instance)
        {
            if (Property is null)
                throw new InvalidOperationException("Animator has no property specified.");

            return control.Bind((AvaloniaProperty<T>)Property, instance, BindingPriority.Animation);
        }

        /// <summary>
        /// Runs the KeyFrames Animation.
        /// </summary>
        internal IDisposable Run(Animation animation, Animatable control, IClock? clock, Action? onComplete)
        {
            var instance = new AnimationInstance<T>(
                animation,
                control,
                this,
                clock ?? control.Clock ?? Clock.GlobalClock,
                onComplete,
                InterpolationHandler);

            return BindAnimation(control, instance);
        }

        /// <summary>
        /// Interpolates in-between two key values given the desired progress time.
        /// </summary>
        public abstract T Interpolate(double progress, T oldValue, T newValue);
    }
}
