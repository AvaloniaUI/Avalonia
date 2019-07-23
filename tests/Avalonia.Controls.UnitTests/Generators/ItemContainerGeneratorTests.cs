// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Controls.UnitTests.Generators
{
    public class ItemContainerGeneratorTests
    {
        [Fact]
        public void Materialize_Should_Create_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = Materialize(target, 0, items);
            var result = containers
                .Select(x => x.ContainerControl)
                .OfType<ContentPresenter>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(items, result);
        }

        [Fact]
        public void ContainerFromIndex_Should_Return_Materialized_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = Materialize(target, 0, items);

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
            var containers = Materialize(target, 0, items);

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
            var containers = Materialize(target, 0, items);

            target.Dematerialize(1, 1);

            Assert.Equal(containers[0].ContainerControl, target.ContainerFromIndex(0));
            Assert.Null(target.ContainerFromIndex(1));
            Assert.Equal(containers[2].ContainerControl, target.ContainerFromIndex(2));
        }

        [Fact]
        public void Dematerialize_Should_Return_Removed_Containers()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = Materialize(target, 0, items);
            var expected = target.Containers.Take(2).ToList();
            var result = target.Dematerialize(0, 2);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void InsertSpace_Should_Alter_Successive_Container_Indexes()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = Materialize(target, 0, items);

            target.InsertSpace(1, 3);

            Assert.Equal(3, target.Containers.Count());
            Assert.Equal(new[] { 0, 4, 5 }, target.Containers.Select(x => x.Index));
        }

        [Fact]
        public void RemoveRange_Should_Alter_Successive_Container_Indexes()
        {
            var items = new[] { "foo", "bar", "baz" };
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var containers = Materialize(target, 0, items);

            var removed = target.RemoveRange(1, 1).Single();

            Assert.Equal(containers[0].ContainerControl, target.ContainerFromIndex(0));
            Assert.Equal(containers[2].ContainerControl, target.ContainerFromIndex(1));
            Assert.Equal(containers[1], removed);
            Assert.Equal(new[] { 0, 1 }, target.Containers.Select(x => x.Index));
        }

        [Fact]
        public void Style_Binding_Should_Be_Able_To_Override_Content()
        {
            var owner = new Decorator();
            var target = new ItemContainerGenerator(owner);
            var container = (ContentPresenter)target.Materialize(0, "foo").ContainerControl;

            Assert.Equal("foo", container.Content);

            container.Bind(
                ContentPresenter.ContentProperty,
                Observable.Never<object>().StartWith("bar"),
                BindingPriority.Style);

            Assert.Equal("bar", container.Content);
        }

        [Fact]
        public void Style_Binding_Should_Be_Able_To_Override_Content_Typed()
        {
            var owner = new Decorator();
            var target = new ItemContainerGenerator<ListBoxItem>(owner, ListBoxItem.ContentProperty, null);
            var container = (ListBoxItem)target.Materialize(0, "foo").ContainerControl;

            Assert.Equal("foo", container.Content);

            container.Bind(
                ContentPresenter.ContentProperty,
                Observable.Never<object>().StartWith("bar"),
                BindingPriority.Style);

            Assert.Equal("bar", container.Content);
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
