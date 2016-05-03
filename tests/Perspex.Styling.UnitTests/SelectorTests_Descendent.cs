// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Data;
using Perspex.LogicalTree;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class SelectorTests_Descendent
    {
        [Fact]
        public void Descendent_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Descendent().OfType<TestLogical2>();

            Assert.True(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public void Descendent_Matches_Control_When_It_Is_Descendent_OfType()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Descendent().OfType<TestLogical3>();

            Assert.True(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public async Task Descendent_Matches_Control_When_It_Is_Descendent_OfType_And_Class()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            grandparent.Classes.Add("foo");
            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendent().OfType<TestLogical3>();
            var activator = selector.Match(child).ObservableResult;

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Descendent_Doesnt_Match_Control_When_It_Is_Descendent_OfType_But_Wrong_Class()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            grandparent.Classes.Add("bar");
            parent.LogicalParent = grandparent;
            parent.Classes.Add("foo");
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendent().OfType<TestLogical3>();
            var activator = selector.Match(child).ObservableResult;

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task Descendent_Matches_Any_Ancestor()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical1();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendent().OfType<TestLogical3>();
            var activator = selector.Match(child).ObservableResult;

            Assert.False(await activator.Take(1));
            parent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            grandparent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            parent.Classes.Remove("foo");
            Assert.True(await activator.Take(1));
            grandparent.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Descendent_Selector_Should_Have_Correct_String_Representation()
        {
            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendent().OfType<TestLogical3>();

            Assert.Equal("TestLogical1.foo TestLogical3", selector.ToString());
        }

        public abstract class TestLogical : ILogical, IStyleable
        {
            public TestLogical()
            {
                Classes = new Classes();
            }

            public event EventHandler<PerspexPropertyChangedEventArgs> PropertyChanged;
            public event EventHandler<LogicalTreeAttachmentEventArgs> AttachedToLogicalTree;
            public event EventHandler<LogicalTreeAttachmentEventArgs> DetachedFromLogicalTree;

            public Classes Classes { get; }

            public string Name { get; set; }

            public bool IsAttachedToLogicalTree { get; }

            public IPerspexReadOnlyList<ILogical> LogicalChildren { get; set; }

            public ILogical LogicalParent { get; set; }

            public Type StyleKey { get; }

            public ITemplatedControl TemplatedParent { get; }

            IPerspexReadOnlyList<string> IStyleable.Classes => Classes;

            IObservable<IStyleable> IStyleable.StyleDetach { get; }

            public object GetValue(PerspexProperty property)
            {
                throw new NotImplementedException();
            }

            public T GetValue<T>(PerspexProperty<T> property)
            {
                throw new NotImplementedException();
            }

            public void SetValue(PerspexProperty property, object value, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public void SetValue<T>(PerspexProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public bool IsSet(PerspexProperty property)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind<T>(PerspexProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public void NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
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
