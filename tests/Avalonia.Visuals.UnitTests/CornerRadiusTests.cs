// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class CornerRadiusTests
    {
        [Fact]
        public void Parse_Parses_Single_Uniform_Radius()
        {
            var result = CornerRadius.Parse("3.4", CultureInfo.InvariantCulture);

            Assert.Equal(new CornerRadius(3.4), result);
        }

        [Fact]
        public void Parse_Parses_Top_Bottom()
        {
            var result = CornerRadius.Parse("1.1,2.2", CultureInfo.InvariantCulture);

            Assert.Equal(new CornerRadius(1.1, 2.2), result);
        }

        [Fact]
        public void Parse_Parses_TopLeft_TopRight_BottomRight_BottomLeft()
        {
            var result = CornerRadius.Parse("1.1,2.2,3.3,4.4", CultureInfo.InvariantCulture);

            Assert.Equal(new CornerRadius(1.1, 2.2, 3.3, 4.4), result);
        }

        [Fact]
        public void Parse_Accepts_Spaces()
        {
            var result = CornerRadius.Parse("1.1 2.2 3.3 4.4", CultureInfo.InvariantCulture);

            Assert.Equal(new CornerRadius(1.1, 2.2, 3.3, 4.4), result);
        }
    }
}