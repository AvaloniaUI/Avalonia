using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;

namespace Avalonia.Rendering.Composition.Animations
{
    
    /// <summary>
    /// A time-based animation with one or more key frames.
    /// These frames are markers, allowing developers to specify values at specific times for the animating property.
    /// KeyFrame animations can be further customized by specifying how the animation interpolates between keyframes.
    /// </summary>
    public abstract class KeyFrameAnimation : CompositionAnimation
    {
        private TimeSpan _duration = TimeSpan.FromMilliseconds(1);

        internal KeyFrameAnimation(Compositor compositor) : base(compositor)
        {
        }
        
        /// <summary>
        /// The delay behavior of the key frame animation.
        /// </summary>
        public AnimationDelayBehavior DelayBehavior { get; set; }

        /// <summary>
        /// Delay before the animation starts after <see cref="CompositionObject.StartAnimation(string , CompositionAnimation)"/> is called.
        /// </summary>
        public System.TimeSpan DelayTime { get; set; }
        
        /// <summary>
        /// The direction the animation is playing.
        /// The Direction property allows you to drive your animation from start to end or end to start or alternate
        /// between start and end or end to start if animation has an <see cref="IterationCount"/> greater than one.
        /// This gives an easy way for customizing animation definitions.
        /// </summary>
        public PlaybackDirection Direction { get; set; }

        /// <summary>
        /// The duration of the animation.
        /// Minimum allowed value is 1ms and maximum allowed value is 24 days.
        /// </summary>
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (_duration < TimeSpan.FromMilliseconds(1) || _duration > TimeSpan.FromDays(1))
                    throw new ArgumentException("Minimum allowed value is 1ms and maximum allowed value is 24 days.");
                _duration = value;
            }
        }
        
        /// <summary>
        /// The iteration behavior for the key frame animation.
        /// </summary>
        public AnimationIterationBehavior IterationBehavior { get; set; }
        
        /// <summary>
        /// The number of times to repeat the key frame animation.
        /// </summary>
        public int IterationCount { get; set; } = 1;
        
        /// <summary>
        /// Specifies how to set the property value when animation is stopped
        /// </summary>
        public AnimationStopBehavior StopBehavior { get; set; }
        
        private protected abstract IKeyFrames KeyFrames { get; }
        
        /// <summary>
        /// Inserts an expression keyframe.
        /// </summary>
        /// <param name="normalizedProgressKey">
        /// The time the key frame should occur at, expressed as a percentage of the animation Duration. Allowed value is from 0.0 to 1.0.
        /// </param>
        /// <param name="value">The expression used to calculate the value of the key frame.</param>
        /// <param name="easingFunction">The easing function to use when interpolating between frames.</param>
        public void InsertExpressionKeyFrame(float normalizedProgressKey, string value,
            Easing? easingFunction = null) =>
            KeyFrames.InsertExpressionKeyFrame(normalizedProgressKey, value, easingFunction ?? Compositor.DefaultEasing);
    }

    /// <summary>
    /// Specifies the animation delay behavior.
    /// </summary>
    public enum AnimationDelayBehavior
    {
        /// <summary>
        /// If a DelayTime is specified, it delays starting the animation according to delay time and after delay
        /// has expired it applies animation to the object property.
        /// </summary>
        SetInitialValueAfterDelay,
        /// <summary>
        /// Applies the initial value of the animation (i.e. the value at Keyframe 0) to the object before the delay time
        /// is elapsed (when there is a DelayTime specified), it then delays starting the animation according to the DelayTime.
        /// </summary>
        SetInitialValueBeforeDelay
    }

    /// <summary>
    /// Specifies if the animation should loop.
    /// </summary>
    public enum AnimationIterationBehavior
    {
        /// <summary>
        /// The animation should loop the specified number of times.
        /// </summary>
        Count,
        /// <summary>
        /// The animation should loop forever.
        /// </summary>
        Forever
    }

    /// <summary>
    /// Specifies the behavior of an animation when it stops.
    /// </summary>
    public enum AnimationStopBehavior
    {
        /// <summary>
        /// Leave the animation at its current value.
        /// </summary>
        LeaveCurrentValue,
        /// <summary>
        /// Reset the animation to its initial value.
        /// </summary>
        SetToInitialValue,
        /// <summary>
        /// Set the animation to its final value.
        /// </summary>
        SetToFinalValue
    }
}
