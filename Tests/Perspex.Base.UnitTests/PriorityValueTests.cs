// -----------------------------------------------------------------------
// <copyright file="PriorityValueTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Xunit;

    public class PriorityValueTests
    {
        [Fact]
        public void Initial_Value_Should_Be_UnsetValue()
        {
            var target = new PriorityValue("Test", typeof(string));

            Assert.Same(PerspexProperty.UnsetValue, target.Value);
        }

        [Fact]
        public void First_Binding_Sets_Value()
        {
            var target = new PriorityValue("Test", typeof(string));

            target.Add(this.Single("foo"), 0);

            Assert.Equal("foo", target.Value);
        }

        [Fact]
        public void Changing_Binding_Should_Set_Value()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<string>("foo");

            target.Add(subject, 0);
            Assert.Equal("foo", target.Value);
            subject.OnNext("bar");
            Assert.Equal("bar", target.Value);
        }

        [Fact]
        public void Binding_With_Lower_Priority_Has_Precedence()
        {
            var target = new PriorityValue("Test", typeof(string));

            target.Add(this.Single("foo"), 1);
            target.Add(this.Single("bar"), 0);
            target.Add(this.Single("baz"), 1);

            Assert.Equal("bar", target.Value);
        }

        [Fact]
        public void Later_Binding_With_Same_Priority_Should_Take_Precedence()
        {
            var target = new PriorityValue("Test", typeof(string));

            target.Add(this.Single("foo"), 1);
            target.Add(this.Single("bar"), 0);
            target.Add(this.Single("baz"), 0);
            target.Add(this.Single("qux"), 1);

            Assert.Equal("baz", target.Value);
        }

        [Fact]
        public void Changing_Binding_With_Lower_Priority_Should_Set_Not_Value()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<string>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 1);
            Assert.Equal("foo", target.Value);
            subject.OnNext("baz");
            Assert.Equal("foo", target.Value);
        }

        [Fact]
        public void UnsetValue_Should_Fall_Back_To_Next_Binding()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<object>("bar");

            target.Add(subject, 0);
            target.Add(this.Single("foo"), 1);

            Assert.Equal("bar", target.Value);

            subject.OnNext(PerspexProperty.UnsetValue);

            Assert.Equal("foo", target.Value);
        }

        [Fact]
        public void Adding_Value_Should_Call_OnNext()
        {
            var target = new PriorityValue("Test", typeof(string));
            bool called = false;

            target.Changed.Subscribe(value => called = (value.Item1 == PerspexProperty.UnsetValue && (string)value.Item2 == "foo"));
            target.Add(this.Single("foo"), 0);

            Assert.True(called);
        }

        [Fact]
        public void Changing_Value_Should_Call_OnNext()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<object>("foo");
            bool called = false;

            target.Add(subject, 0);
            target.Changed.Subscribe(value => called = ((string)value.Item1 == "foo" && (string)value.Item2 == "bar"));
            subject.OnNext("bar");

            Assert.True(called);
        }

        [Fact]
        public void Disposing_A_Binding_Should_Revert_To_Next_Value()
        {
            var target = new PriorityValue("Test", typeof(string));

            target.Add(this.Single("foo"), 0);
            var disposable = target.Add(this.Single("bar"), 0);

            Assert.Equal("bar", target.Value);
            disposable.Dispose();
            Assert.Equal("foo", target.Value);
        }

        [Fact]
        public void Disposing_A_Binding_Should_Remove_BindingEntry()
        {
            var target = new PriorityValue("Test", typeof(string));

            target.Add(this.Single("foo"), 0);
            var disposable = target.Add(this.Single("bar"), 0);

            Assert.Equal(2, target.GetBindings().Count());
            disposable.Dispose();
            Assert.Equal(1, target.GetBindings().Count());
        }

        [Fact]
        public void Completing_A_Binding_Should_Revert_To_Next_Value()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<object>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 0);

            Assert.Equal("bar", target.Value);
            subject.OnCompleted();
            Assert.Equal("foo", target.Value);
        }

        [Fact]
        public void Completing_A_Binding_Should_Remove_BindingEntry()
        {
            var target = new PriorityValue("Test", typeof(string));
            var subject = new BehaviorSubject<object>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 0);

            Assert.Equal(2, target.GetBindings().Count());
            subject.OnCompleted();
            Assert.Equal(1, target.GetBindings().Count());
        }

        /// <summary>
        /// Returns an observable that returns a single value but does not complete.
        /// </summary>
        /// <typeparam name="T">The type of the observable.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The observable.</returns>
        private IObservable<T> Single<T>(T value)
        {
            return Observable.Never<T>().StartWith(value);
        }
    }
}
