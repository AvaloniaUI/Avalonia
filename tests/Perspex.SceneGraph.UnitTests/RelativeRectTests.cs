// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Perspex.SceneGraph.UnitTests
{
    public class RelativeRectTests
    {
        [Fact]
        public void Parse_Should_Accept_Absolute_Value()
        {
            var result = RelativeRect.Parse("4,5,50,60", CultureInfo.InvariantCulture);

            Assert.Equal(new RelativeRect(4, 5, 50, 60, RelativeUnit.Absolute), result);
        }

        [Fact]
        public void Parse_Should_Accept_Relative_Value()
        {
            var result = RelativeRect.Parse("10%, 20%, 40%, 70%", CultureInfo.InvariantCulture);

            Assert.Equal(new RelativeRect(0.1, 0.2, 0.4, 0.7, RelativeUnit.Relative), result);
        }
    }
}
