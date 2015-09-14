// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Perspex.VisualTree
{
    /// <summary>
    /// Tracks the bounds of a control.
    /// </summary>
    /// <remarks>
    /// This class is used by Adorners to track the control that the adorner is attached to.
    /// </remarks>
    public class BoundsTracker
    {
        /// <summary>
        /// Starts tracking the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>An observable that returns the tracked bounds.</returns>
        public IObservable<TransformedBounds> Track(Visual visual)
        {
            return Track(visual, (Visual)visual.GetVisualRoot());
        }

        /// <summary>
        /// Starts tracking the specified visual relative to another control.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="relativeTo">The control that the tracking should be relative to.</param>
        /// <returns>An observable that returns the tracked bounds.</returns>
        public IObservable<TransformedBounds> Track(Visual visual, Visual relativeTo)
        {
            var visuals = visual.GetSelfAndVisualAncestors()
                .TakeWhile(x => x != relativeTo)
                .Reverse();
            var boundsSubscriptions = new List<IObservable<Rect>>();

            foreach (var v in visuals.Cast<Visual>())
            {
                boundsSubscriptions.Add(v.GetObservable(Visual.BoundsProperty));
            }

            var bounds = Observable.CombineLatest(boundsSubscriptions).Select(ExtractBounds);

            // TODO: Track transform and clip rectangle.
            return Observable.Select(bounds, x => new TransformedBounds((Rect)x, (Rect)new Rect(), (Matrix)Matrix.Identity));
        }

        /// <summary>
        /// Sums a collection of rectangles.
        /// </summary>
        /// <param name="rects">The collection of rectangles.</param>
        /// <returns>The summed rectangle.</returns>
        private static Rect ExtractBounds(IList<Rect> rects)
        {
            var position = rects.Select(x => x.Position).Aggregate((a, b) => a + b);
            return new Rect(position, rects.Last().Size);
        }
    }
}
