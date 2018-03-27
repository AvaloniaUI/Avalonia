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



                        // This should be changed later for a much more efficient one 
                        if (sortedkeyValues.Count() > 2)
                        {
                            bool OutsideRange_Start = DoubleUtils.AboutEqual(x, 0.0) || x < 0.0;
                            bool OutsideRange_End = DoubleUtils.AboutEqual(x, 1.0) || x > 1.0;
                            var kyF = sortedkeyValues.ToArray();

                            if (OutsideRange_Start)
                            {
                                firstCue = kyF[0];
                                lastCue = kyF[1];
                            }
                            else if (OutsideRange_End)
                            {
                                var count = kyF.Count();
                                firstCue = kyF[count - 2];
                                lastCue = kyF[count - 1];
                            }
                            else
                            {
                                firstCue = sortedkeyValues.Where(j => j.Key <= x).Last();
                                lastCue = sortedkeyValues.Where(j => j.Key >= x).First();
                            }
                        }
                        else
                        {
                            firstCue = sortedkeyValues.First();
                            lastCue = sortedkeyValues.Last();
                        }

                        double x0, y0, x1, y1;

                        // Swap interpolants if its descending
                        // if (firstCue.Value > lastCue.Value)
                        // // {
                        //     y1 = firstCue.Value;
                        //     x1 = firstCue.Key;
                        //     y0 = lastCue.Value;
                        //     x0 = lastCue.Key;
                        // }
                        // else
                        // {
                        y0 = firstCue.Value;
                        x0 = firstCue.Key;
                        y1 = lastCue.Value;
                        x1 = lastCue.Key;
                        
                        var y = y0 + ((x - x0)/(x1 - x0)) * (y1-y0);

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
