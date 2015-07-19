// -----------------------------------------------------------------------
// <copyright file="BoundsTracker.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.SceneGraph.UnitTests.VisualTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Shapes;
    using Perspex.VisualTree;
    using Xunit;

    public class BoundsTrackerTests
    {
        [Fact]
        public void Should_Track_Bounds()
        {
            var target = new BoundsTracker();
            var control = default(Rectangle);
            var tree = new Decorator
            {
                Padding = new Thickness(10),
                Content = new Decorator
                {
                    Padding = new Thickness(5),
                    Content = (control = new Rectangle
                    {
                        Width = 15,
                        Height = 15,
                    }),
                }
            };

            tree.Measure(Size.Infinity);
            tree.Arrange(new Rect(0, 0, 100, 100));

            var track = target.Track(control, tree);
            var results = new List<TransformedBounds>();
            track.Subscribe(results.Add);

            Assert.Equal(new Rect(15, 15, 15, 15), results.Last().Bounds);

            tree.Padding = new Thickness(15);
            tree.Measure(Size.Infinity);
            tree.Arrange(new Rect(0, 0, 100, 100), true);

            Assert.Equal(new Rect(20, 20, 15, 15), results.Last().Bounds);
        }
    }
}
