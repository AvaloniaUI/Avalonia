// -----------------------------------------------------------------------
// <copyright file="ScrollPresenterTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using Perspex.Controls.Presenters;
    using Xunit;

    public class ScrollContentPresenterTests
    {
        [Fact]
        public void Arrange_Should_Set_Viewport_And_Extent_In_That_Order()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 40, Height = 50 }
            };

            var set = new List<string>();

            target.Measure(new Size(100, 100));

            target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => set.Add("Viewport"));
            target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => set.Add("Extent"));

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new[] { "Viewport", "Extent" }, set);
        }

        [Fact]
        public void Setting_Offset_Should_Invalidate_Arrange()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 40, Height = 50 }
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));
            target.Offset = new Vector(10, 100);

            Assert.True(target.IsMeasureValid);
            Assert.False(target.IsArrangeValid);
        }
    }
}