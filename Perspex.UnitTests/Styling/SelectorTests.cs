// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Styling;
    using Match = Perspex.Styling.Match;

    [TestClass]
    public class SelectorTests
    {
        [TestMethod]
        public void Class_Matches_Control_With_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = control.Select().Class("foo");

            CollectionAssert.AreEqual(new[] { true }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void Class_Doesnt_Match_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "bar" },
            };

            var target = control.Select().Class("foo");

            CollectionAssert.AreEqual(new[] { false }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void Class_Tracks_Additions()
        {
            var control = new Control1();

            var target = control.Select().Class("foo");
            var activator = target.GetActivator();

            CollectionAssert.AreEqual(new[] { false }, activator.Take(1).ToEnumerable().ToArray());
            control.Classes.Add("foo");
            CollectionAssert.AreEqual(new[] { true }, activator.Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void Class_Tracks_Removals()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = control.Select().Class("foo");
            var activator = target.GetActivator();

            CollectionAssert.AreEqual(new[] { true }, activator.Take(1).ToEnumerable().ToArray());
            control.Classes.Remove("foo");
            CollectionAssert.AreEqual(new[] { false }, activator.Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void Id_Matches_Control_With_Correct_Id()
        {
            var control = new Control1 { Id = "foo" };
            var target = control.Select().Id("foo");

            CollectionAssert.AreEqual(new[] { true }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void Id_Doesnt_Match_Control_Of_Wrong_Id()
        {
            var control = new Control1 { Id = "foo" };
            var target = control.Select().Id("bar");

            CollectionAssert.AreEqual(new[] { false }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void When_Id_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1 { Id = "foo" };
            var target = control.Select().Id("foo").SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [TestMethod]
        public void When_Id_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1 { Id = "foo" };
            var target = control.Select().Id("bar").SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        [TestMethod]
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = control.Select().OfType<Control1>();

            CollectionAssert.AreEqual(new[] { true }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = control.Select().OfType<Control1>();

            CollectionAssert.AreEqual(new[] { false }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void When_OfType_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1();
            var target = control.Select().OfType<Control1>().SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [TestMethod]
        public void When_OfType_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1();
            var target = control.Select().OfType<Control2>().SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        public class Control1 : SubscribeCheck
        {
        }

        public class Control2 : SubscribeCheck
        {
        }
    }
}
