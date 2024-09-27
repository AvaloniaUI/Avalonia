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

            var (from, to) = GetKeyFrames(animationTime, neutralValue);

            var progress = (animationTime - from.Time) / (to.Time - from.Time);

            if (to.KeySpline is { } keySpline)
                progress = keySpline.GetSplineProgress(progress);

            return Interpolate(progress, from.Value, to.Value);
        }

        private (KeyFrameInfo From, KeyFrameInfo To) GetKeyFrames(double time, T neutralValue)
        {
            Debug.Assert(Count >= 1);

            // Before or right at the first frame which isn't at time 0.0: interpolate between 0.0 and the first frame.
            var firstFrame = this[0];
            var firstTime = firstFrame.Cue.CueValue;
            if (time <= firstTime && firstTime > 0.0)
            {
                var beforeValue = firstFrame.FillBefore ? GetTypedValue(firstFrame.Value, neutralValue) : neutralValue;
                return (
                    new KeyFrameInfo(0.0, beforeValue, firstFrame.KeySpline),
                    KeyFrameInfo.FromKeyFrame(firstFrame, neutralValue));
            }

            // Between two frames: interpolate between the previous frame and the next frame.
            for (var i = 1; i < Count; ++i)
            {
                var frame = this[i];
                if (time <= frame.Cue.CueValue)
                {
                    return (
                        KeyFrameInfo.FromKeyFrame(this[i - 1], neutralValue),
                        KeyFrameInfo.FromKeyFrame(this[i], neutralValue));
                }
            }

            // Past the last frame which is at time 1.0: interpolate between the last two frames.
            var lastFrame = this[Count - 1];
            if (lastFrame.Cue.CueValue >= 1.0)
            {
                if (Count == 1)
                {
                    var beforeValue = lastFrame.FillBefore ? GetTypedValue(lastFrame.Value, neutralValue) : neutralValue;
                    return (
                        new KeyFrameInfo(0.0, beforeValue, lastFrame.KeySpline),
                        KeyFrameInfo.FromKeyFrame(lastFrame, neutralValue));
                }

                return (
                    KeyFrameInfo.FromKeyFrame(this[Count - 2], neutralValue),
                    KeyFrameInfo.FromKeyFrame(lastFrame, neutralValue));
            }

            // Past the last frame which isn't at time 1.0: interpolate between the last frame and 1.0.
            var afterValue = lastFrame.FillAfter ? GetTypedValue(lastFrame.Value, neutralValue) : neutralValue;
            return (
                KeyFrameInfo.FromKeyFrame(lastFrame, neutralValue),
                new KeyFrameInfo(1.0, afterValue, lastFrame.KeySpline));
        }

        private static T GetTypedValue(object? untypedValue, T neutralValue)
            => untypedValue is T value ? value : neutralValue;

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

        private readonly struct KeyFrameInfo(double time, T value, KeySpline? keySpline)
        {
            public readonly double Time = time;
            public readonly T Value = value;
            public readonly KeySpline? KeySpline = keySpline;

            public static KeyFrameInfo FromKeyFrame(AnimatorKeyFrame source, T neutralValue)
                => new(source.Cue.CueValue, GetTypedValue(source.Value, neutralValue), source.KeySpline);
        }
    }
}
