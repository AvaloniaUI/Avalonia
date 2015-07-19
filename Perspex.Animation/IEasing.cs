// -----------------------------------------------------------------------
// <copyright file="IEasing.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    /// <summary>
    /// Defines the interface for easing functions.
    /// </summary>
    public interface IEasing
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
        object Ease(double progress, object start, object finish);
    }
}
