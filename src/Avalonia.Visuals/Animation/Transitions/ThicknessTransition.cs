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
            return progress
                .Select(p =>
                {
                    var f = Easing.Ease(p);
                    return ((newValue - oldValue) * f) + oldValue;
                });
        }
    }
}
