// -----------------------------------------------------------------------
// <copyright file="CrossFade.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    public class CrossFade : IPageTransition
    {
        public CrossFade(TimeSpan duration)
        {
            this.Duration = duration;
        }

        public TimeSpan Duration { get; }

        public async Task Start(Visual from, Visual to, bool forward)
        {
            var tasks = new List<Task>();

            if (to != null)
            {
                to.Opacity = 0;
            }

            if (from != null)
            {
                tasks.Add(Animate.Property(
                    from,
                    Visual.OpacityProperty,
                    from.Opacity,
                    0,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            if (to != null)
            {
                to.Opacity = 0;
                to.IsVisible = true;

                tasks.Add(Animate.Property(
                    to,
                    Visual.OpacityProperty,
                    0,
                    1,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            await Task.WhenAll(tasks.ToArray());

            if (from != null)
            {
                from.IsVisible = false;
                from.Opacity = 1;
            }

            to.Opacity = 1;
        }
    }
}
