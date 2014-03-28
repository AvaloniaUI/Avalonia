// -----------------------------------------------------------------------
// <copyright file="PriorityValueTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests
{
    using System;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PriorityValueTests
    {
        [TestMethod]
        public void Initial_Value_Should_Be_UnsetValue()
        {
            var target = new PriorityValue();

            Assert.AreSame(PerspexProperty.UnsetValue, target.Value);
        }

        [TestMethod]
        public void First_Binding_Sets_Value()
        {
            var target = new PriorityValue();

            target.Add(this.Single("foo"), 0);

            Assert.AreEqual("foo", target.Value);
        }

        [TestMethod]
        public void Changing_Binding_Should_Set_Value()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<string>("foo");

            target.Add(subject, 0);
            Assert.AreEqual("foo", target.Value);
            subject.OnNext("bar");
            Assert.AreEqual("bar", target.Value);
        }

        [TestMethod]
        public void Binding_With_Lower_Priority_Has_Precedence()
        {
            var target = new PriorityValue();

            target.Add(this.Single("foo"), 1);
            target.Add(this.Single("bar"), 0);
            target.Add(this.Single("baz"), 1);

            Assert.AreEqual("bar", target.Value);
        }

        [TestMethod]
        public void Later_Binding_With_Same_Priority_Should_Take_Precedence()
        {
            var target = new PriorityValue();

            target.Add(this.Single("foo"), 1);
            target.Add(this.Single("bar"), 0);
            target.Add(this.Single("baz"), 0);
            target.Add(this.Single("qux"), 1);

            Assert.AreEqual("baz", target.Value);
        }

        [TestMethod]
        public void Changing_Binding_With_Lower_Priority_Should_Set_Not_Value()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<string>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 1);
            Assert.AreEqual("foo", target.Value);
            subject.OnNext("baz");
            Assert.AreEqual("foo", target.Value);
        }

        [TestMethod]
        public void UnsetValue_Should_Fall_Back_To_Next_Binding()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<object>("bar");

            target.Add(subject, 0);
            target.Add(this.Single("foo"), 1);

            Assert.AreEqual("bar", target.Value);

            subject.OnNext(PerspexProperty.UnsetValue);

            Assert.AreEqual("foo", target.Value);
        }

        [TestMethod]
        public void Adding_Value_Should_Call_OnNext()
        {
            var target = new PriorityValue();
            bool called = false;

            target.Changed.Subscribe(value => called = (value.Item1 == PerspexProperty.UnsetValue && (string)value.Item2 == "foo"));
            target.Add(this.Single("foo"), 0);

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Changing_Value_Should_Call_OnNext()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<object>("foo");
            bool called = false;

            target.Add(subject, 0);
            target.Changed.Subscribe(value => called = ((string)value.Item1 == "foo" && (string)value.Item2 == "bar"));
            subject.OnNext("bar");

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Disposing_A_Binding_Should_Revert_To_Next_Value()
        {
            var target = new PriorityValue();

            target.Add(this.Single("foo"), 0);
            var disposable = target.Add(this.Single("bar"), 0);

            Assert.AreEqual("bar", target.Value);
            disposable.Dispose();
            Assert.AreEqual("foo", target.Value);
        }

        [TestMethod]
        public void Disposing_A_Binding_Should_Remove_BindingEntry()
        {
            var target = new PriorityValue();

            target.Add(this.Single("foo"), 0);
            var disposable = target.Add(this.Single("bar"), 0);

            Assert.AreEqual(2, target.GetBindings().Count());
            disposable.Dispose();
            Assert.AreEqual(1, target.GetBindings().Count());
        }

        [TestMethod]
        public void Completing_A_Binding_Should_Revert_To_Next_Value()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<object>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 0);

            Assert.AreEqual("bar", target.Value);
            subject.OnCompleted();
            Assert.AreEqual("foo", target.Value);
        }

        [TestMethod]
        public void Completing_A_Binding_Should_Remove_BindingEntry()
        {
            var target = new PriorityValue();
            var subject = new BehaviorSubject<object>("bar");

            target.Add(this.Single("foo"), 0);
            target.Add(subject, 0);

            Assert.AreEqual(2, target.GetBindings().Count());
            subject.OnCompleted();
            Assert.AreEqual(1, target.GetBindings().Count());
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
