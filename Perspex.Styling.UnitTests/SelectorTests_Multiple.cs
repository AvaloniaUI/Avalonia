// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Styling;

    ////[TestClass]
    ////public class SelectorTests_Multiple
    ////{
    ////    [TestMethod]
    ////    public void Template_Child_Of_Control_With_Two_Classes()
    ////    {
    ////        var template = new ControlTemplate(parent =>
    ////        {
    ////            return new Border
    ////            {
    ////                Id = "border",
    ////            };
    ////        });

    ////        var control = new Button
    ////        {
    ////            Template = template,
    ////        };

    ////        var selector = new Selector()
    ////            .OfType<Button>()
    ////            .Class("foo")
    ////            .Class("bar")
    ////            .Template()
    ////            .Id("border");

    ////        var border = (Border)((IVisual)control).VisualChildren.Single();
    ////        var values = new List<bool>();
    ////        var activator = selector.GetActivator(border);

    ////        activator.Subscribe(x => values.Add(x));

    ////        CollectionAssert.AreEqual(new[] { false }, values);
    ////        control.Classes.Add("foo", "bar");
    ////        CollectionAssert.AreEqual(new[] { false, true }, values);
    ////        control.Classes.Remove("foo");
    ////        CollectionAssert.AreEqual(new[] { false, true, false }, values);
    ////    }
    ////}
}
