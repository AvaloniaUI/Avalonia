﻿// -----------------------------------------------------------------------
// <copyright file="Pen.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System.Collections.Generic;

    /// <summary>
    /// Describes how a stroke is drawn.
    /// </summary>
    public class Pen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="brush">The brush used to draw.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The length of alternating dashes and gaps.</param>
        public Pen(Brush brush, double thickness, IReadOnlyList<double> dashArray = null)
        {
            this.Brush = brush;
            this.Thickness = thickness;
            this.DashArray = dashArray;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="color">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The length of alternating dashes and gaps.</param>
        public Pen(uint color, double thickness, IReadOnlyList<double> dashArray = null)
        {
            this.Brush = new SolidColorBrush(color);
            this.Thickness = thickness;
            this.DashArray = dashArray;
        }

        /// <summary>
        /// Gets the brush used to draw the stroke.
        /// </summary>
        public Brush Brush { get; }

        /// <summary>
        /// Gets the length of alternating dashes and gaps.
        /// </summary>
        public IReadOnlyList<double> DashArray { get; }

        /// <summary>
        /// Gets the stroke thickness.
        /// </summary>
        public double Thickness { get; }
    }
}
