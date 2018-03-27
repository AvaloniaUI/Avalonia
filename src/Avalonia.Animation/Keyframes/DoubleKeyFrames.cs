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
        public override IObservable<double> DoInterpolation(Animation animation, Animatable control)
            => GetKeyFramesTimer(animation)
              .Select(t =>
              {
                  // Get a pair of keyframes to make the interpolation.
                  var pair = GetKeyFramePairByTime(t);

                  // Do linear parametric interpolation 
                  double y0 = pair.firstKF.Value;
                  double t0 = pair.firstKF.Key;
                  double y1 = pair.lastKF.Value;
                  double t1 = pair.lastKF.Key;

                  // Calculate the final interpolated value
                  return y0 + ((t - t0) / (t1 - t0)) * (y1 - y0);
              });

    }
}
