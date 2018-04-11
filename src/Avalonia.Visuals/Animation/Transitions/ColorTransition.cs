// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;
using System.Reactive.Linq;
using Avalonia.Media;
using System.Numerics;

namespace Avalonia.Animation.Transitions
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="SolidColorBrush"/> types.
    /// TODO: Must be refactored to handle object creation.
    /// </summary>  
    public class ColorTransition : Transition<SolidColorBrush>
    {

        /// <summary>
        /// Defines the <see cref="AnimationPlayState"/> property.
        /// </summary>
        public static readonly DirectProperty<ColorTransition, ColorInterpolationMode> InterpolationModeProperty =
            AvaloniaProperty.RegisterDirect<ColorTransition, ColorInterpolationMode>(
                nameof(InterpolationMode),
                o => o.InterpolationMode,
                (o, v) => o.InterpolationMode = v);

        /// <summary>
        /// Gets or sets the state of the animation for this
        /// control.
        /// </summary>
        public ColorInterpolationMode InterpolationMode
        {
            get { return _interpolationMode; }
            set { SetAndRaise(InterpolationModeProperty, ref _interpolationMode, value); }
        }

        private ColorInterpolationMode _interpolationMode;
  
        public override IObservable<SolidColorBrush> DoTransition(IObservable<double> progress, SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            var oldColor = new Vector4(oldValue.Color.A, oldValue.Color.R, oldValue.Color.G, oldValue.Color.B);
            var newColor = new Vector4(newValue.Color.A, newValue.Color.R, newValue.Color.G, newValue.Color.B);
            oldColor = oldColor / 255f;
            newColor = newColor / 255f;
            var deltaColor = newColor - oldColor;

            switch (InterpolationMode)
            {
                case ColorInterpolationMode.PremultipliedRGB:
                    
                    var premultOV = new Vector4(1, oldColor.W, oldColor.W, oldColor.W);
                    var premultNV = new Vector4(1, newColor.W, newColor.W, newColor.W);
                    
                    oldColor *= premultOV;
                    newColor *= premultNV;

                    return progress
                    .Select(p =>
                    {
                        var time = (float)Easing.Ease(p);
                        var interpolatedColor = (deltaColor * time) + oldColor;
                        return new SolidColorBrush(Color.FromVector4(interpolatedColor, true));
                    });

                case ColorInterpolationMode.RGB:
                    return progress
                    .Select(p =>
                    {
                        var time = (float)Easing.Ease(p);
                        var interpolatedColor = (deltaColor * time) + oldColor;
                        return new SolidColorBrush(Color.FromVector4(interpolatedColor, true));
                    });
                default:
                    throw new NotImplementedException($"{Enum.GetName(typeof(ColorInterpolationMode), InterpolationMode)} color interpolation is not supported.");
            }

        }
 
    }
}
