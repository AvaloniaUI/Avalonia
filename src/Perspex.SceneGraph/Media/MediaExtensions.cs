// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Media
{
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
