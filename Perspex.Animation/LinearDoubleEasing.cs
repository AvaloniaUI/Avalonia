// -----------------------------------------------------------------------
// <copyright file="LinearDoubleEasing.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    /// <summary>
    /// Linearly eases a double value.
    /// </summary>
    public class LinearDoubleEasing : IEasing<double>
    {
        /// <summary>
        /// Returns the value of the transition for the specified progress.
        /// </summary>
        /// <param name="progress">The progress of the transition, from 0 to 1.</param>
        /// <param name="start">The start value of the transition.</param>
        /// <param name="finish">The end value of the transition.</param>
        /// <returns>
        /// A value between <paramref name="start"/> and <paramref name="finish"/> as determined
        /// by <paramref name="progress"/>.
        /// </returns>
        public double Ease(double progress, double start, double finish)
        {
            return ((finish - start) * progress) + start;
        }

        /// <summary>
        /// Returns the value of the transition for the specified progress.
        /// </summary>
        /// <param name="progress">The progress of the transition, from 0 to 1.</param>
        /// <param name="start">The start value of the transition.</param>
        /// <param name="finish">The end value of the transition.</param>
        /// <returns>
        /// A value between <paramref name="start"/> and <paramref name="finish"/> as determined
        /// by <paramref name="progress"/>.
        /// </returns>
        object IEasing.Ease(double progress, object start, object finish)
        {
            return this.Ease(progress, (double)start, (double)finish);
        }
    }
}
