// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.SceneGraph.UnitTests.VisualTree
{
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
                Child = new Decorator
                {
                    Padding = new Thickness(5),
                    Child = control = new Rectangle
                    {
                        Width = 15,
                        Height = 15,
                    },
                }
            };

            tree.Measure(Size.Infinity);
            tree.Arrange(new Rect(0, 0, 100, 100));

            var track = target.Track(control, tree);
            var results = new List<TransformedBounds>();
            track.Subscribe(results.Add);

            Assert.Equal(new Rect(42, 42, 15, 15), results[0].Bounds);

            tree.Padding = new Thickness(15);
            tree.Measure(Size.Infinity);
            tree.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(37, 37, 15, 15), results[1].Bounds);
        }
    }
}
