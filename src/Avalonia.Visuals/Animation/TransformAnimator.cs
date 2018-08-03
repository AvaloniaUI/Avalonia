﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Media;
using Avalonia.Logging;

namespace Avalonia.Animation
{
    /// <summary>
    /// Animator that handles <see cref="Transform"/> properties.
    /// </summary>
    public class TransformAnimator : Animator<double>
    {
        DoubleAnimator childKeyFrames;

        /// <inheritdoc/>
        public override IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch, Action onComplete)
        {
            var ctrl = (Visual)control;

            // Check if the Target Property is Transform derived.
            if (typeof(Transform).IsAssignableFrom(Property.OwnerType))
            {
                if (ctrl.RenderTransform == null)
                {
                    var normalTransform = new TransformGroup();

                    // Add the transforms according to MS Expression Blend's 
                    // default RenderTransform order.

                    normalTransform.Children.Add(new ScaleTransform());
                    normalTransform.Children.Add(new SkewTransform()); 
                    normalTransform.Children.Add(new RotateTransform());
                    normalTransform.Children.Add(new TranslateTransform());

                    ctrl.RenderTransform = normalTransform;
                }

                var renderTransformType = ctrl.RenderTransform.GetType();

                if (childKeyFrames == null)
                {
                    InitializeChildKeyFrames();
                }

                // It's a transform object so let's target that.
                if (renderTransformType == Property.OwnerType)
                {
                    return childKeyFrames.Apply(animation, ctrl.RenderTransform, obsMatch, onComplete);
                }
                // It's a TransformGroup and try finding the target there.
                else if (renderTransformType == typeof(TransformGroup))
                {
                    foreach (Transform transform in ((TransformGroup)ctrl.RenderTransform).Children)
                    {
                        if (transform.GetType() == Property.OwnerType)
                        {
                            return childKeyFrames.Apply(animation, transform, obsMatch, onComplete);
                        }
                    }
                }

                Logger.Warning(
                    LogArea.Animations,
                    control,
                    $"Cannot find the appropriate transform: \"{Property.OwnerType}\" in {control}.");
            }
            else
            {
                Logger.Error(
                    LogArea.Animations,
                    control,
                    $"Cannot apply animation: Target property owner {Property.OwnerType} is not a Transform object.");
            }
            return null;
        }

        void InitializeChildKeyFrames()
        {
            childKeyFrames = new DoubleAnimator();

            foreach (AnimatorKeyFrame keyframe in this)
            {
                childKeyFrames.Add(keyframe);
            }

            childKeyFrames.Property = Property;
        }

        /// <inheritdocs/>
        protected override double DoInterpolation(double time, double neutralValue) => 0;
    }
}
