// -----------------------------------------------------------------------
// <copyright file="PropertyTransition.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

    public class LinearDoubleEasing : IEasing<double>
    {
        public double Ease(double progress, double start, double finish)
        {
            return ((finish - start) * progress) + start;
        }

        public object Ease(double progress, object start, object finish)
        {
            return this.Ease(progress, (double)start, (double)finish);
        }
    }
}
