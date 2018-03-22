// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transitions object that handles properties with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        /// <inheritdocs/>
        public override void DoInterpolation(Animatable control, IObservable<double> progress, double oldValue, double newValue)
        {
            var delta = newValue - oldValue;
            var transition = progress.Select(p =>
            {
                return Easing.Ease(p) * delta + oldValue;
            });
            control.Bind(Property, transition.Select(p=>(object)p), Data.BindingPriority.Animation);
        }
    }
}
