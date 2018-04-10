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

            // Check if the Target Property is Transform derived.
            if (typeof(Transform).IsAssignableFrom(Property.OwnerType) && ctrl.RenderTransform != null)
            {
                var renderTransformType = ctrl.RenderTransform.GetType();
                
                if (childKeyFrames == null)
                {
                    InitializeInternalDoubleKeyFrames();
                }

                // It's only 1 transform object so let's target that.
                if (renderTransformType == Property.OwnerType)
                {
                    return childKeyFrames.Apply(animation, ctrl.RenderTransform, obsMatch);
                }
                // Try if the control's RenderTransform is a TransformGroup and find 
                // the target there.
                else if (renderTransformType == typeof(TransformGroup))
                {
                    foreach (Transform transform in ((TransformGroup)ctrl.RenderTransform).Children)
                    {
                        if (transform.GetType() == Property.OwnerType)
                        {
                            return childKeyFrames.Apply(animation, transform, obsMatch);
                        }
                    }
                }

                //throw new InvalidOperationException($"TransformKeyFrame hasn't found an appropriate Transform object with type {Property.OwnerType} in target {control}.");

                return null;

            }
            else
            {
                throw new Exception($"Unsupported property {Property}");
            }
        }

        void InitializeInternalDoubleKeyFrames()
        {
            childKeyFrames = new DoubleKeyFrames();

            foreach (KeyFrame keyframe in this)
            {
                childKeyFrames.Add(keyframe);
            }

            childKeyFrames.Property = Property;
        }

        /// <inheritdocs/>
        public override IObservable<double> DoInterpolation(Animation animation, Animatable control)
        {
            return null;
        }
    }
}
