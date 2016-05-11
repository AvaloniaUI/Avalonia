// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AttachedPropertyTests
    {
        [Fact]
        public void IsAttached_Returns_True()
        {
            var property = new AttachedProperty<string>(
                "Foo",
                typeof(Class1),
                new StyledPropertyMetadata<string>());

            Assert.True(property.IsAttached);
        }

        private class Class1 : AvaloniaObject
        {
        }
    }
}
