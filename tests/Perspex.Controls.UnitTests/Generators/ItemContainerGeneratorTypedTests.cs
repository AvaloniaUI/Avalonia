// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Generators;
using Xunit;

namespace Perspex.Controls.UnitTests.Generators
{
    public class ItemContainerGeneratorTypedTests
    {
        [Fact]
        public void Materialize_Should_Create_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator<ListBoxItem>(owner, ListBoxItem.ContentProperty);
            var containers = target.Materialize(0, items, null);
            var result = containers
                .Select(x => x.ContainerControl)
                .OfType<ListBoxItem>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(items, result);
        }
    }
}
