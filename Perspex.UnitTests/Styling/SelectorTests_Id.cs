// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Styling;

    [TestClass]
    public class SelectorTests_Id
    {
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
        public void Id_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { Id = "foo", TemplatedParent = new TemplatedControl1() };
            var target = control.Select().Id("foo");

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

        public class Control1 : TestControlBase
        {
        }

        public class TemplatedControl1 : ITemplatedControl
        {
            public IEnumerable<IVisual> VisualChildren
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
