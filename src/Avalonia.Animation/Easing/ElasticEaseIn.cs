// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Animation.Utils;
using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a damped sine function.
    /// </summary>
    public class ElasticEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return Math.Sin(13d * EasingUtils.HALFPI * p) * Math.Pow(2d, 10d * (p - 1));            
        }
    }
}
