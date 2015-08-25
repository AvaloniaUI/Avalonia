// -----------------------------------------------------------------------
// <copyright file="TrackTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Primitives
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using Layout;
    using Moq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.LogicalTree;
    using Perspex.Platform;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Splat;
    using Templates;
    using Xunit;

    public class TrackTests
    {
        [Fact]
        public void Measure_Should_Return_Thumb_DesiredWidth_In_Vertical_Orientation()
        {
            var thumb = new Thumb
            {
                Width = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(12, 0), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Thumb_DesiredHeight_In_Horizontal_Orientation()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(0, 12), target.DesiredSize);
        }

        [Fact]
        public void Should_Arrange_Thumb_In_Horizontal_Orientation()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
                Minimum = 100,
                Maximum = 200,
                Value = 150,
                ViewportSize = 50,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(25, 0, 50, 12), thumb.Bounds);
        }

        [Fact]
        public void Should_Arrange_Thumb_In_Vertical_Orientation()
        {
            var thumb = new Thumb
            {
                Width = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
                Minimum = 100,
                Maximum = 300,
                Value = 150,
                ViewportSize = 50,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 18, 12, 25), thumb.Bounds);
        }

        [Fact]
        public void Thumb_Should_Fill_Track_When_Minimum_Equals_Maximum()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
                Minimum = 100,
                Maximum = 100,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 100, 12), thumb.Bounds);
        }
    }
}
