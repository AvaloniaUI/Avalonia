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
    public class SelectorTests_Class
    {
        [TestMethod]
        public void Class_Matches_Control_With_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = new Selector().Class("foo");

            Assert.IsTrue(ActivatorValue(target, control));
        }

        [TestMethod]
        public void Class_Doesnt_Match_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "bar" },
            };

            var target = new Selector().Class("foo");

            Assert.IsFalse(ActivatorValue(target, control));
        }

        [TestMethod]
        public void Class_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
                TemplatedParent = new Mock<ITemplatedControl>().Object,
            };

            var target = new Selector().Class("foo");

            Assert.IsFalse(ActivatorValue(target, control));
        }

        [TestMethod]
        public void Class_Tracks_Additions()
        {
            var control = new Control1();

            var target = new Selector().Class("foo");
            var activator = target.GetActivator(control);

            Assert.IsFalse(ActivatorValue(target, control));
            control.Classes.Add("foo");
            Assert.IsTrue(ActivatorValue(target, control));
        }

        [TestMethod]
        public void Class_Tracks_Removals()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = new Selector().Class("foo");
            var activator = target.GetActivator(control);

            Assert.IsTrue(ActivatorValue(target, control));
            control.Classes.Remove("foo");
            Assert.IsFalse(ActivatorValue(target, control));
        }

        private static bool ActivatorValue(Selector selector, IStyleable control)
        {
            return selector.GetActivator(control).Take(1).ToEnumerable().Single();
        }

        public class Control1 : TestControlBase
        {
        }
    }
}
