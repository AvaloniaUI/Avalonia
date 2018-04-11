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
    /// TODO: Must be refactored to handle object creation every frame.
    /// </summary>  
    public class ColorTransition : Transition<SolidColorBrush>
    {
        public override IObservable<SolidColorBrush> DoTransition(IObservable<double> progress, SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            var oldColor = new Vector4(oldValue.Color.R, oldValue.Color.G, oldValue.Color.B, oldValue.Color.A);
            var newColor = new Vector4(newValue.Color.R, newValue.Color.G, newValue.Color.B, oldValue.Color.A);
            oldColor = oldColor / 255f;
            newColor = newColor / 255f;
            var deltaColor = newColor - oldColor;
            var premultOV = new Vector4(oldColor.W, oldColor.W, oldColor.W, 1);
            var premultNV = new Vector4(newColor.W, newColor.W, newColor.W, 1);

            oldColor *= premultOV;
            newColor *= premultNV;

            return progress
            .Select(p =>
            {
                var time = (float)Easing.Ease(p);
                var interpolatedColor = (deltaColor * time) + oldColor;
                return new SolidColorBrush(Color.FromVector4(interpolatedColor, true));
            });
        }
    }
}
