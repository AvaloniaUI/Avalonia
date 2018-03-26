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
        public override IDisposable DoInterpolation(Animation animation, Animatable control, Dictionary<double, double> sortedkeyValues)
        {
            var timer = Timing.GetTimer(animation.Duration, animation.Delay);


            var interp = timer.Select(p =>
                {
                    // Handle the errors rather naively, for now.
                    try
                    {
                        var x = animation.Easing.Ease(p);

                        // Get a pair of keyframes to make the interpolation.
                        KeyValuePair<double, double> firstCue, lastCue;

                        firstCue = sortedkeyValues.First();
                        lastCue = sortedkeyValues.Last();

                        // This should be changed later for a much more efficient one 
                        if (sortedkeyValues.Count() > 2)
                        {
                            bool isWithinRange_Start = DoubleUtils.AboutEqual(x, 0.0) || x > 0.0;
                            bool isWithinRange_End = DoubleUtils.AboutEqual(x, 1.0) || x < 1.0;

                            if (isWithinRange_Start && isWithinRange_End)
                            { 

                                firstCue = sortedkeyValues.Where(j => j.Key <= x).Last();
                                lastCue = sortedkeyValues.Where(j=> j.Key >= firstCue.Key).First();
                            }
                            else if (!isWithinRange_Start)
                            {
                                firstCue = sortedkeyValues.First();
                                lastCue = sortedkeyValues.Skip(1).First();
                            }
                            else if (!isWithinRange_End)
                            {
                                firstCue = sortedkeyValues.Skip(sortedkeyValues.Count() - 1).First();
                                lastCue = sortedkeyValues.Last();
                            }
                            else
                            {
                                throw new InvalidOperationException
                                    ($"Can't find KeyFrames within the specified Easing time {x}");
                            }
                        }

                        // Piecewise Linear interpolation, courtesy of wikipedia
                        var y0 = firstCue.Value;
                        var x0 = firstCue.Key;
                        var y1 = lastCue.Value;
                        var x1 = lastCue.Key;
                        var y = ((y0 * (x1 - x)) + (y1 * (x - x0))) / x1 - x0;

                        return y;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        return 1;
                    }
                });


            return control.Bind(Property, interp.Select(p => (object)p), BindingPriority.Animation);
        }


    }
}
