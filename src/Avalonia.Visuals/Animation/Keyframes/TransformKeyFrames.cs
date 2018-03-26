using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Media;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Key frames that handles <see cref="double"/> properties.
    /// </summary>
    public class TransformKeyFrames : KeyFrames<double>
    {
        DoubleKeyFrames childKeyFrames;
        
        /// <inheritdoc/>
        public override IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch)
        {
            var ctrl = (Visual)control;

            // Check if the AvaloniaProperty is Transform derived.
            if (typeof(Transform).IsAssignableFrom(Property.OwnerType))
            {
                var renderTransformType = ctrl.RenderTransform.GetType();

                // It's only 1 transform object so let's target that.
                if (renderTransformType == Property.OwnerType)
                {
                    var targetTransform = Convert.ChangeType(ctrl.RenderTransform, Property.OwnerType);

                    if (childKeyFrames == null)
                    {
                        childKeyFrames = new DoubleKeyFrames();

                        foreach (KeyFrame k in this)
                        {
                            childKeyFrames.Add(k);
                        }

                        childKeyFrames.Property = Property;
                    }

                    return childKeyFrames.Apply(animation, ctrl.RenderTransform, obsMatch);
                }
                if (renderTransformType == typeof(TransformGroup))
                {
                    foreach (Transform t in ((TransformGroup)ctrl.RenderTransform).Children)
                    {
                        if (renderTransformType == Property.OwnerType)
                        {

                        }
                    }

                    // not existing in the transform

                }
            }
            else
            {
                throw new InvalidProgramException($"Unsupported property {Property}");
            }

            return null;
        }

        /// <inheritdocs/>
        public override IDisposable DoInterpolation(Animation animation, Animatable control, Dictionary<double, double> keyValues)
        {
            return Timing.GetTimer(animation.Duration, animation.Delay).Subscribe();
        }
    }
}
