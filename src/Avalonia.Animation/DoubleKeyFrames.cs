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
        protected override double DoInterpolation(double t, double neutralValue)
        {
            var pair = GetKFPairAndIntraKFTime(t);
            double y0, y1;

            var firstKF = pair.KFPair.FirstKeyFrame;
            var secondKF = pair.KFPair.SecondKeyFrame;

            if (firstKF.Value.isNeutral)
                y0 = neutralValue;
            else
                y0 = firstKF.Value.TargetValue;

            if (secondKF.Value.isNeutral)
                y1 = neutralValue;
            else
                y1 = secondKF.Value.TargetValue;

            // Do linear parametric interpolation 
            return y0 + (pair.IntraKFTime) * (y1 - y0);
        }
    }
}
