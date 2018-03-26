using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Base class for KeyFrames 
    /// </summary>
    public abstract class KeyFrames<T> : AvaloniaList<KeyFrame>, IKeyFrames
    {

        /// <summary>
        /// Target property.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        /// Enable if the derived class will do the verification of  
        /// its keyframes.
        internal bool IsVerfifiedAndConverted;

        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch)
        {
            if(obsMatch == null) return null;
            
            if (!IsVerfifiedAndConverted)
                VerifyKeyFrames(animation, typeof(T));

            return obsMatch
                .Where(p => p == true)
                .Subscribe(_ => DoInterpolation(animation, control, ConvertedValues));
        }


        /// <summary>
        /// Interpolates the given keyframes to the control.
        /// </summary>
        public abstract IDisposable DoInterpolation(Animation animation,
                                                    Animatable control,
                                                    Dictionary<double, T> keyValues);

        internal Dictionary<double, T> ConvertedValues = new Dictionary<double, T>();

        /// <summary>
        /// Verifies keyframe value types.
        /// </summary>
        private void VerifyKeyFrames(Animation animation, Type type)
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

                ConvertedValues.Add(_normalizedCue.CueValue, convertedValue);

            }

            // This can be optional if we ever try to make
            // the default start and end values to be the
            // property's prior value.
            SortKeyFrameCues(ConvertedValues);

            IsVerfifiedAndConverted = true;

        }

        private void SortKeyFrameCues(Dictionary<double, T> convertedValues)
        {
            SortKeyFrameCues(convertedValues.ToDictionary((k) => k.Key, (v) => (object)v.Value));
        }

        internal void SortKeyFrameCues(Dictionary<double, object> convertedValues)
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            foreach (var converted in ConvertedValues.Keys)
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
            ConvertedValues = ConvertedValues.OrderBy(p => p.Key)
                                             .ToDictionary((k) => k.Key, (v) => v.Value);

        }
    }
}
