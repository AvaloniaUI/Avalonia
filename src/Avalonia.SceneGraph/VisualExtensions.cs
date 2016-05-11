// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia
{
    /// <summary>
    /// Extension methods for <see cref="IVisual"/>.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        public static Point PointToClient(this IVisual visual, Point point)
        {
            var p = GetRootAndPosition(visual);
            return p.Item1.PointToClient(point + p.Item2);
        }

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        public static Point PointToScreen(this IVisual visual, Point point)
        {
            var p = GetRootAndPosition(visual);
            return p.Item1.PointToScreen(point + p.Item2);
        }

        /// <summary>
        /// Gets the root of the control's visual tree and the position of the control 
        /// in the root's coordinate space.
        /// </summary>
        /// <param name="v">The visual.</param>
        /// <returns>A tuple containing the root and the position of the control.</returns>
        private static Tuple<IRenderRoot, Vector> GetRootAndPosition(IVisual v)
        {
            var result = new Vector();

            while (!(v is IRenderRoot))
            {
                result = new Vector(result.X + v.Bounds.X, result.Y + v.Bounds.Y);
                v = v.VisualParent;

                if (v == null)
                {
                    throw new InvalidOperationException("Control is not attached to visual tree.");
                }
            }

            return Tuple.Create((IRenderRoot)v, result);
        }
    }
}
