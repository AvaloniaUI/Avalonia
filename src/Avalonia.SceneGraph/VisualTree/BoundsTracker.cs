// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;

namespace Avalonia.VisualTree
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
                .Cast<Visual>();

            var bounds = visuals.Select(v => v.GetObservable(Visual.BoundsProperty)).CombineLatest().Select(ExtractBounds);
            
            return visuals.Select(v => v.GetObservable(Visual.BoundsProperty)
                .CombineLatest(v.GetObservable(Visual.RenderTransformProperty), v.GetObservable(Visual.TransformOriginProperty),
                (b, rt, to) => new { b, t = new { rt, to = to.ToPixels(new Size(b.Width, b.Height)) } })).CombineLatest()
                .Select(visualsInfo =>
                    new
                    {
                        b = ExtractBounds(visualsInfo.Select(visualInfo => visualInfo.b)),
                        m = visualsInfo
                            .Select(visualInfo =>
                            (visualInfo.t.rt?.Value ?? Matrix.Identity)
                            )
                            .Aggregate(Matrix.Identity, (acc, mat) => mat * acc)
                    }
                ).
                Select(transformedBounds => new TransformedBounds(transformedBounds.b, new Rect(), transformedBounds.m));
        }

        /// <summary>
        /// Sums a collection of rectangles.
        /// </summary>
        /// <param name="rects">The collection of rectangles.</param>
        /// <returns>The summed rectangle.</returns>
        private static Rect ExtractBounds(IEnumerable<Rect> rects)
        {
            var position = rects.Select(x => x.Position).Aggregate((a, b) => a + b);
            return new Rect(position, rects.First().Size);
        }
    }
}
