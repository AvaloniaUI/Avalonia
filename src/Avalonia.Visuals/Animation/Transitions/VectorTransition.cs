// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Vector"/> type.
    /// </summary>  
    public class VectorTransition : Transition<Vector>
    {
        /// <inheritdocs/>
        public override IObservable<Vector> DoTransition(IObservable<double> progress, Vector oldValue, Vector newValue)
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
