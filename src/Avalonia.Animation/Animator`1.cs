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
        private readonly SortedList<double, (T, bool isNeutral)> _convertedKeyframes = new SortedList<double, (T, bool)>();

        private bool _isVerfifiedAndConverted;

        /// <summary>
        /// Gets or sets the target property for the keyframe.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        public Animator()
        {
            // Invalidate keyframes when changed.
            this.CollectionChanged += delegate { _isVerfifiedAndConverted = false; };
        }

        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch)
        {
            if (!_isVerfifiedAndConverted)
                VerifyConvertKeyFrames(animation);

            return obsMatch
                // Ignore triggers when global timers are paused.
                .Where(p => p && Timing.GetGlobalPlayState() != PlayState.Pause)
                .Subscribe(_ =>
                {
                    var timerObs = RunKeyFrames(animation, control);
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
            KeyValuePair<double, (T, bool)> firstCue, lastCue;
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
            var intraframeTime = (t - t0) / (t1 - t0);
            return (intraframeTime, new KeyFramePair<T>(firstCue, lastCue));
        }


        /// <summary>
        /// Runs the KeyFrames Animation.
        /// </summary>
        private IDisposable RunKeyFrames(Animation animation, Animatable control)
        {
            var stateMachine = new AnimatorStateMachine<T>();
            stateMachine.Initialize(animation, control, this);

            Timing.AnimationStateTimer
                        .TakeWhile(_ => !stateMachine._unsubscribe)
                        .Subscribe(p => stateMachine.Step(p, DoInterpolation));

            return control.Bind(Property, stateMachine, BindingPriority.Animation);
        }

        /// <summary>
        /// Interpolates a value given the desired time.
        /// </summary>
        protected abstract T DoInterpolation(double time, T neutralValue);

        /// <summary>
        /// Verifies and converts keyframe values according to this class's target type.
        /// </summary>
        private void VerifyConvertKeyFrames(Animation animation)
        {
            var typeConv = TypeDescriptor.GetConverter(typeof(T));

            foreach (AnimatorKeyFrame k in this)
            {
                if (k.Value == null)
                {
                    throw new ArgumentNullException($"KeyFrame value can't be null.");
                }
                if (!typeConv.CanConvertTo(k.Value.GetType()))
                {
                    throw new InvalidCastException($"KeyFrame value doesnt match property type.");
                }

                T convertedValue = (T)typeConv.ConvertTo(k.Value, typeof(T));
                
                _convertedKeyframes.Add(k.Cue.CueValue, (convertedValue, false));
            }

            AddNeutralKeyFramesIfNeeded();
            _isVerfifiedAndConverted = true;

        }

        private void AddNeutralKeyFramesIfNeeded()
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            // Make start and end keyframe mandatory.
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
                AddNeutralKeyFrames(hasStartKey, hasEndKey, _convertedKeyframes);
        }

        private void AddNeutralKeyFrames(bool hasStartKey, bool hasEndKey, IDictionary<double, (T, bool)> convertedKeyframes)
        {
            if (!hasStartKey)
            {
                convertedKeyframes.Add(0.0d, (default(T), true));
            }

            if (!hasEndKey)
            {
                convertedKeyframes.Add(1.0d, (default(T), true));
            }
        }
    }
}
