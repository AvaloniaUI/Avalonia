using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Collections;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for KeyFrames objects
    /// </summary>
    public abstract class Animator<T> : AvaloniaList<AnimatorKeyFrame>, IAnimator
    {
        /// <summary>
        /// List of type-converted keyframes.
        /// </summary>
        private readonly List<AnimatorKeyFrame> _convertedKeyframes = new List<AnimatorKeyFrame>();
 
        private bool _isVerifiedAndConverted;

        /// <summary>
        /// Gets or sets the target property for the keyframe.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        public Animator()
        {
            // Invalidate keyframes when changed.
             this.CollectionChanged += delegate { _isVerifiedAndConverted = false; };
        }

        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete)
        {
             if (!_isVerifiedAndConverted)
                VerifyConvertKeyFrames();

            return match
                .Where(p => p)
                .Subscribe(_ =>
                {
                    var timerObs = RunKeyFrames(animation, control, clock, onComplete);
                });
        }

        /// <summary>
        /// Get the nearest pair of cue-time ordered keyframes 
        /// according to the given time parameter that is relative to the
        /// total animation time and the normalized intra-keyframe pair time 
        /// (i.e., the normalized time between the selected keyframes, relative to the
        /// time parameter).
        /// </summary>
        /// <param name="animationTime">The time parameter, relative to the total animation time</param>
        protected (double IntraKFTime, KeyFramePair<T> KFPair) GetKFPairAndIntraKFTime(double animationTime)
        {
            AnimatorKeyFrame firstKeyframe, lastKeyframe ;
            int kvCount = _convertedKeyframes.Count;
            if (kvCount > 2)
            {
                if (animationTime <= 0.0)
                {
                    firstKeyframe = _convertedKeyframes[0];
                    lastKeyframe = _convertedKeyframes[1];
                }
                else if (animationTime >= 1.0)
                {
                    firstKeyframe = _convertedKeyframes[_convertedKeyframes.Count - 2];
                    lastKeyframe = _convertedKeyframes[_convertedKeyframes.Count - 1];
                }
                else
                {
                    int index = FindClosestBeforeKeyFrame(animationTime);
                    firstKeyframe = _convertedKeyframes[index];
                    lastKeyframe = _convertedKeyframes[index + 1];
                }
            }
            else
            {
                firstKeyframe = _convertedKeyframes[0];
                lastKeyframe = _convertedKeyframes[1];
            }

            double t0 = firstKeyframe.Cue.CueValue;
            double t1 = lastKeyframe.Cue.CueValue;
            var intraframeTime = (animationTime - t0) / (t1 - t0);
            var firstFrameData = (firstKeyframe.GetTypedValue<T>(), firstKeyframe.isNeutral);
            var lastFrameData = (lastKeyframe.GetTypedValue<T>(), lastKeyframe.isNeutral);
            return (intraframeTime, new KeyFramePair<T>(firstFrameData, lastFrameData));
        }

        private int FindClosestBeforeKeyFrame(double time)
        {
            int FindClosestBeforeKeyFrame(int startIndex, int length)
            {
                if (length == 0 || length == 1)
                {
                    return startIndex;
                }

                int middle = startIndex + (length / 2);

                if (_convertedKeyframes[middle].Cue.CueValue < time)
                {
                    return FindClosestBeforeKeyFrame(middle, length - middle);
                }
                else if (_convertedKeyframes[middle].Cue.CueValue > time)
                {
                    return FindClosestBeforeKeyFrame(startIndex, middle - startIndex);
                }
                else
                {
                    return middle;
                }
            }

            return FindClosestBeforeKeyFrame(0, _convertedKeyframes.Count);
        }

        /// <summary>
        /// Runs the KeyFrames Animation.
        /// </summary>
        private IDisposable RunKeyFrames(Animation animation, Animatable control, IClock clock, Action onComplete)
        {
            var instance = new AnimationInstance<T>(
                animation,
                control,
                this,
                clock ?? control.Clock ?? Clock.GlobalClock,
                onComplete,
                DoInterpolation);
            return control.Bind<T>((AvaloniaProperty<T>)Property, instance, BindingPriority.Animation);
        }

        /// <summary>
        /// Interpolates a value given the desired time.
        /// </summary>
        protected abstract T DoInterpolation(double time, T neutralValue);

        /// <summary>
        /// Verifies, converts and sorts keyframe values according to this class's target type.
        /// </summary>
        private void VerifyConvertKeyFrames()
        {
            foreach (AnimatorKeyFrame keyframe in this)
            {
                _convertedKeyframes.Add(keyframe);
            }

            AddNeutralKeyFramesIfNeeded();

            _isVerifiedAndConverted = true;
        }

        private void AddNeutralKeyFramesIfNeeded()
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            // Check if there's start and end keyframes.
            foreach (var frame in _convertedKeyframes)
            {
                if (frame.Cue.CueValue == 0.0d)
                {
                    hasStartKey = true;
                }
                else if (frame.Cue.CueValue == 1.0d)
                {
                    hasEndKey = true;
                }
            }

            if (!hasStartKey || !hasEndKey)
                AddNeutralKeyFrames(hasStartKey, hasEndKey);
        }

        private void AddNeutralKeyFrames(bool hasStartKey, bool hasEndKey)
        {
            if (!hasStartKey)
            {
                _convertedKeyframes.Insert(0, new AnimatorKeyFrame(null, new Cue(0.0d)) { Value = default(T), isNeutral = true });
            }

            if (!hasEndKey)
            {
                _convertedKeyframes.Add(new AnimatorKeyFrame(null, new Cue(1.0d)) { Value = default(T), isNeutral = true });
            }
        }
    }
}
