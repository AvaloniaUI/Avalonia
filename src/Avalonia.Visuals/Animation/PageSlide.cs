// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transitions between two pages by sliding them horizontally.
    /// </summary>
    public class PageSlide : IPageTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        public PageSlide()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public PageSlide(TimeSpan duration)
        {
            Duration = duration;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// If true, the new page is slid in from the right, or if false from the left.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        public async Task Start(IVisual from, IVisual to, bool forward)
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
                    Duration).ToTask());
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
                    Duration).ToTask());
            }

            await Task.WhenAll(tasks.ToArray());

            if (from != null)
            {
                from.IsVisible = false;
            }
        }

        /// <summary>
        /// Gets the common visual parent of the two control.
        /// </summary>
        /// <param name="from">The from control.</param>
        /// <param name="to">The to control.</param>
        /// <returns>The common parent.</returns>
        /// <exception cref="ArgumentException">
        /// The two controls do not share a common parent.
        /// </exception>
        /// <remarks>
        /// Any one of the parameters may be null, but not both.
        /// </remarks>
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
