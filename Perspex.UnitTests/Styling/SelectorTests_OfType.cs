// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System.Linq;
    using System.Reactive.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Perspex.Controls;
    using Perspex.Styling;

    [TestClass]
    public class SelectorTests_OfType
    {
        [TestMethod]
        public void OfType_Priority_Is_Style()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();

            Assert.AreEqual(BindingPriority.Style, target.Priority);
        }

        [TestMethod]
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();

            Assert.IsTrue(ActivatorValue(target, control));
        }

        [TestMethod]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = new Selector().OfType<Control1>();

            Assert.IsFalse(ActivatorValue(target, control));
        }

        [TestMethod]
        public void OfType_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().OfType<Control1>();

            Assert.IsFalse(ActivatorValue(target, control));
        }

        [TestMethod]
        public void When_OfType_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>().SubscribeCheck();

            var result = target.GetActivator(control).ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [TestMethod]
        public void When_OfType_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control2>().SubscribeCheck();

            var result = target.GetActivator(control).ToEnumerable().Take(1).ToArray();

            Assert.AreEqual(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        private static bool ActivatorValue(Selector selector, IStyleable control)
        {
            return selector.GetActivator(control).Take(1).ToEnumerable().Single();
        }

        public class Control1 : TestControlBase
        {
        }

        public class Control2 : TestControlBase
        {
        }
    }
}
