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

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Base class for KeyFrames objects
    /// </summary>
    public abstract class KeyFrames<T> : AvaloniaList<KeyFrame>, IKeyFrames
    {

        /// <summary>
        /// Target property.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        /// <summary>
        /// List of type-converted keyframes.
        /// </summary>
        public Dictionary<double, T> ConvertedKeyframes = new Dictionary<double, T>();

        private bool IsVerfifiedAndConverted;


        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch)
        {
            if (!IsVerfifiedAndConverted)
                VerifyConvertKeyFrames(animation, typeof(T));

            return obsMatch
                .Where(p => p == true)
                // Ignore triggers when global timers are paused.
                .Where(p=> Timing.GetGlobalPlayState() == PlayState.Run)
                .Subscribe(_ =>
                {
                    var interp = DoInterpolation(animation, control)
                                .Select(p => (object)p);

                    control.Bind(Property, interp, BindingPriority.Animation);
                });
        }

        /// <summary>
        /// Get the nearest pair of cue-time ordered keyframes 
        /// according to the given time parameter.  
        /// </summary>
        public KeyFramePair<T> GetKeyFramePairByTime(double t)
        {
            KeyValuePair<double, T> firstCue, lastCue;
            int kvCount = ConvertedKeyframes.Count();
            if (kvCount > 2)
            {
                if (DoubleUtils.AboutEqual(t, 0.0) || t < 0.0)
                {
                    firstCue = ConvertedKeyframes.First();
                    lastCue = ConvertedKeyframes.Skip(1).First();
                }
                else if (DoubleUtils.AboutEqual(t, 1.0) || t > 1.0)
                {
                    firstCue = ConvertedKeyframes.Skip(kvCount - 2).First();
                    lastCue = ConvertedKeyframes.Last();
                }
                else
                {
                    firstCue = ConvertedKeyframes.Where(j => j.Key <= t).Last();
                    lastCue = ConvertedKeyframes.Where(j => j.Key >= t).First();
                }
            }
            else
            {
                firstCue = ConvertedKeyframes.First();
                lastCue = ConvertedKeyframes.Last();
            }
            return new KeyFramePair<T>(firstCue, lastCue);
        }


        /// <summary>
        /// Returns an observable timer with the specific Animation
        /// duration and delay and applies the Animation's easing function.
        /// </summary>
        public IObservable<(double Time, Animatable Target)> 
            SetupAnimation(Animation animation, Animatable control) =>
                        Timing.GetAnimationsTimer(control, animation.Duration, animation.Delay)
                              .Select(t => (animation.Easing.Ease(t), control));

        /// <summary>
        /// Interpolates the given keyframes to the control.
        /// </summary>
        public abstract IObservable<T> DoInterpolation(Animation animation,
                                                       Animatable control);


        /// <summary>
        /// Verifies and converts keyframe values according to this class type parameter.
        /// </summary>
        private void VerifyConvertKeyFrames(Animation animation, Type type)
        {
            var typeConv = TypeDescriptor.GetConverter(type);

            foreach (KeyFrame k in this)
            {

                if (k.Value == null)
                {
                    throw new ArgumentNullException($"KeyFrame value can't be null.");
                }
                if (!typeConv.CanConvertTo(k.Value.GetType()))
                {
                    throw new InvalidCastException($"KeyFrame value doesnt match property type.");
                }

                T convertedValue = (T)typeConv.ConvertTo(k.Value, type);

                Cue _normalizedCue = k.Cue;

                if (k.timeSpanSet)
                {
                    _normalizedCue = new Cue(k.KeyTime.Ticks / animation.Duration.Ticks);
                }

                ConvertedKeyframes.Add(_normalizedCue.CueValue, convertedValue);

            }

            SortKeyFrameCues(ConvertedKeyframes);
            IsVerfifiedAndConverted = true;

        }

        private void SortKeyFrameCues(Dictionary<double, T> convertedValues)
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            // this can be optional later, by making the default start/end keyframes
            // to have a neutral value (a.k.a. the value prior to the animation).
            foreach (var converted in ConvertedKeyframes.Keys)
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

            if (!hasStartKey && !hasEndKey)
                throw new InvalidOperationException
                    ($"{this.GetType().Name} must have a starting (0% cue) and ending (100% cue) keyframe.");

            // Sort Cues, in case they don't order it by themselves.
            ConvertedKeyframes = ConvertedKeyframes.OrderBy(p => p.Key)
                                                   .ToDictionary((k) => k.Key, (v) => v.Value);
        }

    }
}
