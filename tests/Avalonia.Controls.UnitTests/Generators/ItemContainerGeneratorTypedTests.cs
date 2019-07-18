// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Xunit;

namespace Avalonia.Controls.UnitTests.Generators
{
    public class ItemContainerGeneratorTypedTests
    {
        [Fact]
        public void Materialize_Should_Create_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator<ListBoxItem>(owner, ListBoxItem.ContentProperty, null);
            var containers = Materialize(target, 0, items);
            var result = containers
                .Select(x => x.ContainerControl)
                .OfType<ListBoxItem>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(items, result);
        }

        private IList<ItemContainerInfo> Materialize(
            IItemContainerGenerator generator,
            int index,
            string[] items)
        {
            var result = new List<ItemContainerInfo>();

            foreach (var item in items)
            {
                var container = generator.Materialize(index++, item);
                result.Add(container);
            }

            return result;
        }
    }
}
