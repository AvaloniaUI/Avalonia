// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Perspex.Collections;
using Perspex.Styling;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class SelectorTests_Child
    {
        [Fact]
        public void Child_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Child().OfType<TestLogical2>();

            Assert.True(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public void Child_Doesnt_Match_Control_When_It_Is_Grandchild_OfType()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Child().OfType<TestLogical3>();

            Assert.False(selector.Match(child).ImmediateResult);
        }

        [Fact]
        public async Task Child_Matches_Control_When_It_Is_Child_OfType_And_Class()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = new Selector().OfType<TestLogical1>().Class("foo").Child().OfType<TestLogical2>();
            var activator = selector.Match(child).ObservableResult;

            Assert.False(await activator.Take(1));
            parent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            parent.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Child_Doesnt_Match_Control_When_It_Has_No_Parent()
        {
            var control = new TestLogical3();
            var selector = new Selector().OfType<TestLogical1>().Child().OfType<TestLogical3>();

            Assert.False(selector.Match(control).ImmediateResult);
        }

        public abstract class TestLogical : ILogical, IStyleable
        {
            public TestLogical()
            {
                Classes = new Classes();
            }

            public Classes Classes { get; }

            public string Name { get; set; }

            public IPerspexReadOnlyList<ILogical> LogicalChildren { get; set; }

            public ILogical LogicalParent { get; set; }

            public Type StyleKey { get; }

            public ITemplatedControl TemplatedParent { get; }

            public IPropertyBag InheritanceParent
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public void SetValue(PerspexProperty property, object value, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public IObservable<object> GetObservable(PerspexProperty property)
            {
                throw new NotImplementedException();
            }

            public bool IsRegistered(PerspexProperty property)
            {
                throw new NotImplementedException();
            }

            public void ClearValue(PerspexProperty property)
            {
                throw new NotImplementedException();
            }

            public object GetValue(PerspexProperty property)
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

            public IObservable<T> GetObservable<T>(PerspexProperty<T> property)
            {
                throw new NotImplementedException();
            }

            public T GetValue<T>(PerspexProperty<T> property)
            {
                throw new NotImplementedException();
            }

            public void SetValue<T>(PerspexProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public IDisposable BindTwoWay(PerspexProperty property, PerspexObject source, PerspexProperty sourceProperty, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public IDisposable BindTwoWay(PerspexProperty property, ISubject<object> source, BindingPriority priority = BindingPriority.LocalValue)
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
