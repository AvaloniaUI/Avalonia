using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Data;
using System.Reactive.Disposables;

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
        private readonly SortedList<double, (AnimatorKeyFrame, bool isNeutral)> _convertedKeyframes = new SortedList<double, (AnimatorKeyFrame, bool)>();

        private bool isVerfifiedAndConverted;

        /// <summary>
        /// Gets or sets the target property for the keyframe.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        public Animator()
        {
            // Invalidate keyframes when changed.
            this.CollectionChanged += delegate { isVerfifiedAndConverted = false; };
        }

        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IObservable<bool> Match, Action onComplete)
        {
            if (!isVerfifiedAndConverted)
                VerifyConvertKeyFrames();

            return Match
                .Where(p => p)
                .Subscribe(_ =>
                {
                    var timerObs = RunKeyFrames(animation, control, onComplete);
                });
        }

        /// <summary>
        /// Get the nearest pair of cue-time ordered keyframes 
        /// according to the given time parameter that is relative to the
        /// total animation time and the normalized intra-keyframe pair time 
        /// (i.e., the normalized time between the selected keyframes, relative to the
        /// time parameter).
        /// </summary>
        /// <param name="t">The time parameter, relative to the total animation time</param>
        protected (double IntraKFTime, KeyFramePair<T> KFPair) GetKFPairAndIntraKFTime(double t)
        {
            KeyValuePair<double, (AnimatorKeyFrame frame, bool isNeutral)> firstCue, lastCue;
            int kvCount = _convertedKeyframes.Count;
            if (kvCount > 2)
            {
                if (DoubleUtils.AboutEqual(t, 0.0) || t < 0.0)
                {
                    firstCue = _convertedKeyframes.First();
                    lastCue = _convertedKeyframes.Skip(1).First();
                }
                else if (DoubleUtils.AboutEqual(t, 1.0) || t > 1.0)
                {
                    firstCue = _convertedKeyframes.Skip(kvCount - 2).First();
                    lastCue = _convertedKeyframes.Last();
                }
                else
                {
                    firstCue = _convertedKeyframes.Last(j => j.Key <= t);
                    lastCue = _convertedKeyframes.First(j => j.Key >= t);
                }
            }
            else
            {
                firstCue = _convertedKeyframes.First();
                lastCue = _convertedKeyframes.Last();
            }

            double t0 = firstCue.Key;
            double t1 = lastCue.Key;
            if (t0 != t1)
            {
                var intraframeTime = (t - t0) / (t1 - t0);
                var firstFrameData = (firstCue.Value.frame.GetTypedValue<T>(), firstCue.Value.isNeutral);
                var lastFrameData = (lastCue.Value.frame.GetTypedValue<T>(), lastCue.Value.isNeutral);
                return (intraframeTime, new KeyFramePair<T>(firstFrameData, lastFrameData));
            }
            else
            {
                var frameData = (firstCue.Value.frame.GetTypedValue<T>(), firstCue.Value.isNeutral);
                return (0.0, new KeyFramePair<T>(frameData, frameData));
            }
        }


        /// <summary>
        /// Runs the KeyFrames Animation.
        /// </summary>
        private IDisposable RunKeyFrames(Animation animation, Animatable control, Action onComplete)
        {
            var stateMachine = new AnimationsEngine<T>(animation, control, this, onComplete);

            Timing.AnimationsTimer
                        .TakeWhile(_ => !stateMachine.unsubscribe)
                        .Subscribe(p => stateMachine.Step(p, DoInterpolation));

            return control.Bind<T>((AvaloniaProperty<T>)Property, stateMachine, BindingPriority.Animation);
        }

        /// <summary>
        /// Interpolates a value given the desired time.
        /// </summary>
        protected abstract T DoInterpolation(double time, T neutralValue);

        /// <summary>
        /// Verifies and converts keyframe values according to this class's target type.
        /// </summary>
        private void VerifyConvertKeyFrames()
        {
            foreach (AnimatorKeyFrame keyframe in this)
            {
                _convertedKeyframes.Add(keyframe.Cue.CueValue, (keyframe, false));
            }

            AddNeutralKeyFramesIfNeeded();
            isVerfifiedAndConverted = true;

        }

        private void AddNeutralKeyFramesIfNeeded()
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            // Check if there's start and end keyframes.
            foreach (var converted in _convertedKeyframes.Keys)
            {
                if (DoubleUtils.AboutEqual(converted, 0.0))
                {
                    hasStartKey = true;
                }
                else if (DoubleUtils.AboutEqual(converted, 1.0))
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
                _convertedKeyframes.Add(0.0d, (new AnimatorKeyFrame { Value = default(T) }, true));
            }

            if (!hasEndKey)
            {
                _convertedKeyframes.Add(1.0d, (new AnimatorKeyFrame { Value = default(T) }, true));
            }
        }
    }
}
