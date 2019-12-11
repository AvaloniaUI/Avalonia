// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="float"/> types.
    /// </summary>  
    public class FloatTransition : Transition<float>
    {
        /// <inheritdocs/>
        public override IObservable<float> DoTransition(IObservable<double> progress, float oldValue, float newValue)
        {
            var delta = newValue - oldValue;
            return progress
                .Select(p => (float)Easing.Ease(p) * delta + oldValue);
        }
    }
}
