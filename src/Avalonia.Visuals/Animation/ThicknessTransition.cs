// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Thickness"/> type.
    /// </summary>  
    public class ThicknessTransition : Transition<Thickness>
    {
        /// <inheritdocs/>
        public override IObservable<Thickness> DoTransition(IObservable<double> progress, Thickness oldValue, Thickness newValue)
        {
            var deltaL = newValue.Left - oldValue.Left;
            var deltaT = newValue.Top - oldValue.Top;
            var deltaR = newValue.Right - oldValue.Right;
            var deltaB = newValue.Bottom - oldValue.Bottom;

            return progress
                .Select(p => 
                {
                    var f = Easing.Ease(p);
                    var nL = f * deltaL + oldValue.Left;
                    var nT = f * deltaT + oldValue.Right;
                    var nR = f * deltaR + oldValue.Top;
                    var nB = f * deltaB + oldValue.Bottom;
                    return new Thickness(nL, nT, nR, nB);
                });
        }
    }
}
