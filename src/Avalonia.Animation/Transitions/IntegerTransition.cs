// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="int"/> types.
    /// </summary>  
    public class IntegerTransition : Transition<int>
    {
        /// <inheritdocs/>
        public override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue)
        {
            var delta = newValue - oldValue;
            return progress
                .Select(p => (int)(Easing.Ease(p) * delta + oldValue));
        }
    }
}
