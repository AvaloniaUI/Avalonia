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
    using Perspex.Collections;
    using Perspex.Styling;
    using Xunit;

    public class SelectorTests_Parent
    {
        [Fact]
        public void Parent_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Parent().OfType<TestLogical2>();

            Assert.True(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public void Parent_Doesnt_Match_Control_When_It_Is_Grandchild_OfType()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Parent().OfType<TestLogical3>();

            Assert.False(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public async Task Parent_Matches_Control_When_It_Is_Child_OfType_And_Class()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Class("foo").Parent().OfType<TestLogical2>();
            var activator = selector.Match(child).ObservableResult;

            Assert.False(await activator.Take(1));
            parent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            parent.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        public abstract class TestLogical : ILogical, IStyleable
        {
            public TestLogical()
            {
                this.Classes = new Classes();
            }

            public Classes Classes { get; }
            public string Name { get; set; }
            public IPerspexReadOnlyList<ILogical> LogicalChildren { get; set; }
            public ILogical LogicalParent { get; set; }
            public Type StyleKey { get; }
            public ITemplatedControl TemplatedParent { get; }

            public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public void SetValue(PerspexProperty property, object value, BindingPriority priority)
            {
                throw new NotImplementedException();
            }
        }

        public class TestLogical1 : TestLogical
        {
        }

        public class TestLogical2 : TestLogical
        {
        }

        public class TestLogical3 : TestLogical
        {
        }
    }
}
