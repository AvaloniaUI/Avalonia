// -----------------------------------------------------------------------
// <copyright file="BoundsTracker.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    public class BoundsTracker
    {
        public IObservable<TransformedBounds> Track(Visual visual)
        {
            return this.Track(visual, (Visual)visual.GetVisualRoot());
        }

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

        private static Rect ExtractBounds(IList<Rect> rects)
        {
            var position = rects.Select(x => x.Position).Aggregate((a, b) => a + b);
            return new Rect(position, rects.Last().Size);
        }
    }
}
