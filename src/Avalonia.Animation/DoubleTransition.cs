// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        /// <inheritdocs/>
        public override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue)
        {
            var delta = newValue - oldValue;
            return progress
                .Select(p => Easing.Ease(p) * delta + oldValue);
        }
    }
}
