// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class RectTests
    {
        [Fact]
        public void Union_Should_Return_Correct_Value_For_Intersecting_Rects()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(50, 50, 100, 100));

            Assert.Equal(new Rect(0, 0, 150, 150), result);
        }

        [Fact]
        public void Union_Should_Return_Correct_Value_For_NonIntersecting_Rects()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 100, 100));

            Assert.Equal(new Rect(0, 0, 250, 250), result);
        }

        [Fact]
        public void Union_Should_Ignore_Empty_This_rect()
        {
            var result = new Rect(0, 0, 0, 0).Union(new Rect(150, 150, 100, 100));

            Assert.Equal(new Rect(150, 150, 100, 100), result);
        }

        [Fact]
        public void Union_Should_Ignore_Empty_Other_rect()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 0, 0));

            Assert.Equal(new Rect(0, 0, 100, 100), result);
        }
    }
}
