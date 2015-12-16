// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Generators;
using Xunit;

namespace Perspex.Controls.UnitTests.Generators
{
    public class ItemContainerGeneratorTests
    {
        [Fact]
        public void Materialize_Should_Create_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null);
            var result = containers
                .Select(x => x.ContainerControl)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(items, result);
        }

        [Fact]
        public void ContainerFromIndex_Should_Return_Materialized_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null).ToList();

            Assert.Equal(containers[0].ContainerControl, target.ContainerFromIndex(0));
            Assert.Equal(containers[1].ContainerControl, target.ContainerFromIndex(1));
            Assert.Equal(containers[2].ContainerControl, target.ContainerFromIndex(2));
        }

        [Fact]
        public void IndexFromContainer_Should_Return_Index()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null).ToList();

            Assert.Equal(0, target.IndexFromContainer(containers[0].ContainerControl));
            Assert.Equal(1, target.IndexFromContainer(containers[1].ContainerControl));
            Assert.Equal(2, target.IndexFromContainer(containers[2].ContainerControl));
        }

        [Fact]
        public void Dematerialize_Should_Remove_Container()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null).ToList();

            target.Dematerialize(1, 1);

            Assert.Equal(containers[0].ContainerControl, target.ContainerFromIndex(0));
            Assert.Equal(null, target.ContainerFromIndex(1));
            Assert.Equal(containers[2].ContainerControl, target.ContainerFromIndex(2));
        }

        [Fact]
        public void Dematerialize_Should_Return_Removed_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null);
            var expected = target.Containers.Take(2).ToList();
            var result = target.Dematerialize(0, 2);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RemoveRange_Should_Alter_Successive_Container_Indexes()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = target.Materialize(0, items, null).ToList();

            var removed = target.RemoveRange(1, 1).Single();

            Assert.Equal(containers[0].ContainerControl, target.ContainerFromIndex(0));
            Assert.Equal(containers[2].ContainerControl, target.ContainerFromIndex(1));
            Assert.Equal(containers[1], removed);
        }
    }
}
