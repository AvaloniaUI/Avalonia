// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Styling;

    [TestClass]
    public class SelectorTests_InTemplateOf
    {
        [TestMethod]
        public void InTemplateOf_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1 { TemplatedParent = new TemplatedControl1() };
            var target = control.Select().InTemplateOf<TemplatedControl1>();

            CollectionAssert.AreEqual(new[] { true }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void InTemplateOf_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control1 { TemplatedParent = new TemplatedControl1() };
            var target = control.Select().InTemplateOf<TemplatedControl2>();

            CollectionAssert.AreEqual(new[] { false }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        [TestMethod]
        public void When_InTemplateOf_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1 { TemplatedParent = new TemplatedControl1() };
            var target = control.Select().InTemplateOf<TemplatedControl1>().SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [TestMethod]
        public void When_InTemplateOf_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1 { TemplatedParent = new TemplatedControl1() };
            var target = control.Select().InTemplateOf<TemplatedControl2>().SubscribeCheck();

            var result = target.GetActivator().ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        public class Control1 : SubscribeCheck
        {
        }

        public class TemplatedControl1 : TemplatedControl
        {
            protected override Size MeasureContent(Size availableSize)
            {
                throw new NotImplementedException();
            }
        }

        public class TemplatedControl2 : TemplatedControl
        {
            protected override Size MeasureContent(Size availableSize)
            {
                throw new NotImplementedException();
            }
        }
    }
}
