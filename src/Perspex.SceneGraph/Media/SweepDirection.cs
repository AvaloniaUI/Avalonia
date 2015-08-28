// -----------------------------------------------------------------------
// <copyright file="SweepDirection.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Defines the direction an which elliptical arc is drawn.
    /// </summary>
    public enum SweepDirection
    {
        /// <summary>
        /// Specifies that arcs are drawn in a counter clockwise (negative-angle) direction.
        /// </summary>
        CounterClockwise,

        /// <summary>
        /// Specifies that arcs are drawn in a clockwise (positive-angle) direction.
        /// </summary>
        Clockwise,
    }
}
