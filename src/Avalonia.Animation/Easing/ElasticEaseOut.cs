// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a damped sine function.
    /// </summary>
    public class ElasticEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return Math.Sin(-13d * EasingUtils.HALFPI * (p + 1)) * Math.Pow(2d, -10d * p) + 1d;
        }
    }
}
