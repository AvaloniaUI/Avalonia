// -----------------------------------------------------------------------
// <copyright file="StyleTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;

    [TestClass]
    public class StyleTests
    {
        [TestMethod]
        public void Style_Should_Update_And_Restore_Value()
        {
            Style style = new Style
            {
                Selector = x => x.Select<TextBlock>().Class("foo"),
                Setters = new[]
                {
                    new Setter
                    {
                        Property = TextBlock.TextProperty,
                        Value = "Foo",
                    }
                },
            };

            TextBlock textBlock = new TextBlock
            {
                Text = "Original",
            };

            style.Attach(textBlock);
            Assert.AreEqual("Original", textBlock.Text);
            textBlock.Classes.Add("foo");
            Assert.AreEqual("Foo", textBlock.Text);
            textBlock.Classes.Remove("foo");
            Assert.AreEqual("Original", textBlock.Text);
        }

        [TestMethod]
        public void Later_Styles_Should_Override_Earlier()
        {
            Style style1 = new Style
            {
                Selector = x => x.Select<TextBlock>().Class("foo"),
                Setters = new[]
                {
                    new Setter
                    {
                        Property = TextBlock.TextProperty,
                        Value = "Foo",
                    }
                },
            };

            Style style2 = new Style
            {
                Selector = x => x.Select<TextBlock>().Class("foo"),
                Setters = new[]
                {
                    new Setter
                    {
                        Property = TextBlock.TextProperty,
                        Value = "Bar",
                    }
                },
            };

            TextBlock textBlock = new TextBlock
            {
                Text = "Original",
            };

            List<string> values = new List<string>();
            textBlock.GetObservable(TextBlock.TextProperty).Subscribe(x => values.Add(x));

            style1.Attach(textBlock);
            style2.Attach(textBlock);
            textBlock.Classes.Add("foo");
            textBlock.Classes.Remove("foo");

            CollectionAssert.AreEqual(new[] { "Original", "Bar", "Original" }, values);
        }
    }
}
