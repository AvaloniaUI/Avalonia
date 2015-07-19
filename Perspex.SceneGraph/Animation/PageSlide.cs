// -----------------------------------------------------------------------
// <copyright file="PageSlide.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;
    using Perspex.Media;
    using Perspex.VisualTree;

    public class PageSlide : IPageTransition
    {
        public PageSlide(TimeSpan duration)
        {
            this.Duration = duration;
        }

        public TimeSpan Duration { get; }

        public async Task Start(Visual from, Visual to, bool forward)
        {
            var tasks = new List<Task>();
            var parent = GetVisualParent(from, to);
            var distance = parent.Bounds.Width;

            if (from != null)
            {
                var transform = new TranslateTransform();
                from.RenderTransform = transform;
                tasks.Add(Animate.Property(
                    transform,
                    TranslateTransform.XProperty,
                    0.0,
                    forward ? -distance : distance,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            if (to != null)
            {
                var transform = new TranslateTransform();
                to.RenderTransform = transform;
                to.IsVisible = true;
                tasks.Add(Animate.Property(
                    transform,
                    TranslateTransform.XProperty,
                    forward ? distance : -distance,
                    0.0,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            await Task.WhenAll(tasks.ToArray());

            if (from != null)
            {
                from.IsVisible = false;
            }
        }

        private static IVisual GetVisualParent(IVisual from, IVisual to)
        {
            var p1 = (from ?? to).VisualParent;
            var p2 = (to ?? from).VisualParent;

            if (p1 != null && p2 != null && p1 != p2)
            {
                throw new ArgumentException("Controls for PageSlide must have same parent.");
            }

            return p1;
        }
    }
}
