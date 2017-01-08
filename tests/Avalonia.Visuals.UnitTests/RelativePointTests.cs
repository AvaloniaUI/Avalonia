// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class RelativePointTests
    {
        [Fact]
        public void Parse_Should_Accept_Absolute_Value()
        {
            var result = RelativePoint.Parse("4,5", CultureInfo.InvariantCulture);

            Assert.Equal(new RelativePoint(4, 5, RelativeUnit.Absolute), result);
        }

        [Fact]
        public void Parse_Should_Accept_Relative_Value()
        {
            var result = RelativePoint.Parse("25%, 50%", CultureInfo.InvariantCulture);

            Assert.Equal(new RelativePoint(0.25, 0.5, RelativeUnit.Relative), result);
        }
    }
}
