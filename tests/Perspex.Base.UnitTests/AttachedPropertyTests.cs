// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

namespace Perspex.Base.UnitTests
{
    public class AttachedPropertyTests
    {
        [Fact]
        public void IsAttached_Returns_True()
        {
            var property = new AttachedProperty<string>(
                "Foo",
                typeof(Class1),
                false,
                new StyledPropertyMetadata(null));

            Assert.True(property.IsAttached);
        }

        private class Class1 : PerspexObject
        {
        }
    }
}
