// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class ThicknessTests
    {
        [Fact]
        public void Parse_Parses_Single_Uniform_Size()
        {
            var result = Thickness.Parse("1.2", CultureInfo.InvariantCulture);

            Assert.Equal(new Thickness(1.2), result);
        }

        [Fact]
        public void Parse_Parses_Horizontal_Vertical()
        {
            var result = Thickness.Parse("1.2,3.4", CultureInfo.InvariantCulture);

            Assert.Equal(new Thickness(1.2, 3.4), result);
        }

        [Fact]
        public void Parse_Parses_Left_Top_Right_Bottom()
        {
            var result = Thickness.Parse("1.2, 3.4, 5, 6", CultureInfo.InvariantCulture);

            Assert.Equal(new Thickness(1.2, 3.4, 5, 6), result);
        }

        [Fact]
        public void Parse_Accepts_Spaces()
        {
            var result = Thickness.Parse("1.2 3.4 5 6", CultureInfo.InvariantCulture);

            Assert.Equal(new Thickness(1.2, 3.4, 5, 6), result);
        }
    }
}
