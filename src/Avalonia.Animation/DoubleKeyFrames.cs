using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Key frames that handles <see cref="double"/> properties.
    /// </summary>
    public class DoubleKeyFrames : KeyFrames<double>
    {

        /// <inheritdocs/>
        protected override double DoInterpolation(double t)
        {
            var pair = GetKFPairAndIntraKFTime(t);
            double y0 = pair.KFPair.FirstKeyFrame.Value;
            double y1 = pair.KFPair.SecondKeyFrame.Value;

            // Do linear parametric interpolation 
            return y0 + (pair.IntraKFTime) * (y1 - y0);
        }
    }
}
