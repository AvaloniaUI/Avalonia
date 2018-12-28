// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="CornerRadius"/> type.
    /// </summary>  
    public class CornerRadiusTransition : Transition<CornerRadius>
    {
        /// <inheritdocs/>
        public override IObservable<CornerRadius> DoTransition(IObservable<double> progress, CornerRadius oldValue, CornerRadius newValue)
        {
            return progress
                .Select(p =>
                {
                    var f = Easing.Ease(p);

                    var deltaTL = newValue.TopLeft - oldValue.TopLeft;
                    var deltaTR = newValue.TopRight - oldValue.TopRight;
                    var deltaBR = newValue.BottomRight - oldValue.BottomRight;
                    var deltaBL = newValue.BottomLeft - oldValue.BottomLeft;

                    var nTL = f * deltaTL + oldValue.TopLeft;
                    var nTR = f * deltaTR + oldValue.TopRight;
                    var nBR = f * deltaBR + oldValue.BottomRight;
                    var nBL = f * deltaBL + oldValue.BottomLeft;

                    return new CornerRadius(nTL, nTR, nBR, nBL);
                });
        }
    }
}
