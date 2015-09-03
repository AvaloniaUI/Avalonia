// -----------------------------------------------------------------------
// <copyright file="MediaExtensions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;

    /// <summary>
    /// Provides extension methods for Perspex media.
    /// </summary>
    public static class MediaExtensions
    {
        /// <summary>
        /// Calculates scaling based on a <see cref="Stretch"/> value.
        /// </summary>
        /// <param name="stretch">The stretch mode.</param>
        /// <param name="destinationSize">The size of the destination viewport.</param>
        /// <param name="sourceSize">The size of the source.</param>
        /// <returns>A vector with the X and Y scaling factors.</returns>
        public static Vector CalculateScaling(this Stretch stretch, Size destinationSize, Size sourceSize)
        {
            double scaleX = 1;
            double scaleY = 1;

            if (stretch != Stretch.None)
            {
                scaleX = destinationSize.Width / sourceSize.Width;
                scaleY = destinationSize.Height / sourceSize.Height;

                switch (stretch)
                {
                    case Stretch.Uniform:
                        scaleX = scaleY = Math.Min(scaleX, scaleY);
                        break;
                    case Stretch.UniformToFill:
                        scaleX = scaleY = Math.Max(scaleX, scaleY);
                        break;
                }
            }

            return new Vector(scaleX, scaleY);
        }
    }
}
