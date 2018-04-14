using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Key frames that handles <see cref="double"/> properties.
    /// </summary>
    public class DoubleKeyFrames : KeyFrames<double>
    {

        /// <inheritdocs/>
        public override double DoInterpolation(double t)
        {
            var pair = GetKeyFramePairByTime(t);

            double y0 = pair.FirstKeyFrame.Value;
            double t0 = pair.FirstKeyFrame.Key;
            double y1 = pair.SecondKeyFrame.Value;
            double t1 = pair.SecondKeyFrame.Key;

            // Do linear parametric interpolation 
            return y0 + ((t - t0) / (t1 - t0)) * (y1 - y0);
        }
    }
}
