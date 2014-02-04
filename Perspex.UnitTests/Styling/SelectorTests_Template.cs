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
    using Perspex.Styling;

    [TestClass]
    public class SelectorTests_Template
    {
        [TestMethod]
        public void Foo()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = control.Select().Class("foo");

            CollectionAssert.AreEqual(new[] { true }, target.GetActivator().Take(1).ToEnumerable().ToArray());
        }

        public class Control1 : SubscribeCheck
        {
        }
    }
}
