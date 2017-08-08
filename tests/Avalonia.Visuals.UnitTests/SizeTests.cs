// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class SizeTests
    {
        [Fact]
        public void Should_Produce_Correct_Aspect_Ratio()
        {
            var result = new Size(3, 2).AspectRatio;

            Assert.Equal(1.5, result);
        }

        [Fact]
        public void Dividing_Should_Produce_Scaling_Factor()
        {
            var result = new Size(15, 10) / new Size(5, 5);

            Assert.Equal(new Vector(3, 2), result);
        }
    }
}
