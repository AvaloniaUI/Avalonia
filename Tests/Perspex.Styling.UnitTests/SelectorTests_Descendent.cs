// -----------------------------------------------------------------------
// <copyright file="SelectorTests_Descendent.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Moq;
    using Perspex.Collections;
    using Perspex.Styling;
    using Xunit;

    public class SelectorTests_Descendent
    {
        [Fact]
        public async Task Descendent_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new Mock<TestLogical1>();
            var child = new Mock<TestLogical2>();
            var childStyleable = child.As<IStyleable>();

            child.Setup(x => x.LogicalParent).Returns(parent.Object);

            var selector = new Selector().OfType(parent.Object.GetType()).Descendent().OfType(child.Object.GetType());
            var activator = selector.GetActivator(childStyleable.Object);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Descendent_Matches_Control_When_It_Is_Descendent_OfType()
        {
            var grandparent = new Mock<TestLogical1>();
            var parent = new Mock<TestLogical2>();
            var child = new Mock<TestLogical3>();

            parent.Setup(x => x.LogicalParent).Returns(grandparent.Object);
            child.Setup(x => x.LogicalParent).Returns(parent.Object);

            var selector = new Selector().OfType(grandparent.Object.GetType()).Descendent().OfType(child.Object.GetType());
            var activator = selector.GetActivator(child.Object);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Descendent_Matches_Control_When_It_Is_Descendent_OfType_And_Class()
        {
            var grandparent = new Mock<TestLogical1>();
            var parent = new Mock<TestLogical2>();
            var child = new Mock<TestLogical3>();

            grandparent.Setup(x => x.Classes).Returns(new Classes("foo"));
            parent.Setup(x => x.LogicalParent).Returns(grandparent.Object);
            parent.Setup(x => x.Classes).Returns(new Classes());
            child.Setup(x => x.LogicalParent).Returns(parent.Object);

            var selector = new Selector().OfType(grandparent.Object.GetType()).Class("foo").Descendent().OfType(child.Object.GetType());
            var activator = selector.GetActivator(child.Object);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Descendent_Doesnt_Match_Control_When_It_Is_Descendent_OfType_But_Wrong_Class()
        {
            var grandparent = new Mock<TestLogical1>();
            var parent = new Mock<TestLogical2>();
            var child = new Mock<TestLogical3>();

            grandparent.Setup(x => x.Classes).Returns(new Classes("bar"));
            parent.Setup(x => x.LogicalParent).Returns(grandparent.Object);
            parent.Setup(x => x.Classes).Returns(new Classes("foo"));
            child.Setup(x => x.LogicalParent).Returns(parent.Object);

            var selector = new Selector().OfType<TestLogical1>().Class("foo").Descendent().OfType<TestLogical3>();
            var activator = selector.GetActivator(child.Object);

            Assert.False(await activator.Take(1));
        }

        public abstract class TestLogical : ILogical, IStyleable
        {
            public abstract Classes Classes { get; }
            public abstract string Name { get; }
            public abstract IPerspexReadOnlyList<ILogical> LogicalChildren { get; }
            public abstract ILogical LogicalParent { get; }
            public Type StyleKey { get; }
            public abstract ITemplatedControl TemplatedParent { get; }
            public abstract IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority = BindingPriority.LocalValue);
        }

        public abstract class TestLogical1 : TestLogical
        {
        }

        public abstract class TestLogical2 : TestLogical
        {
        }

        public abstract class TestLogical3 : TestLogical
        {
        }
    }
}
