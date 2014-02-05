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
    using Match = Perspex.Styling.Match;

    [TestClass]
    public class SelectorTests_Template
    {
        [TestMethod]
        public void Control_In_Template_Is_Not_Matched_Without_Template_Selector()
        {
            var templatedControl = new Mock<ITemplatedControl>();
            var styleable = templatedControl.As<IStyleable>();
            this.BuildVisualTree(templatedControl);

            var border = (Border)templatedControl.Object.VisualChildren.Single();
            var selector = border.Select().OfType<Border>();

            Assert.AreEqual(false, ActivatorValue(selector));
        }

        [TestMethod]
        public void Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var templatedControl = new Mock<ITemplatedControl>();
            var styleable = templatedControl.As<IStyleable>();
            this.BuildVisualTree(templatedControl);

            var border = (Border)templatedControl.Object.VisualChildren.Single();
            var selector = border.Select().Template().OfType<Border>();

            Assert.AreEqual(true, ActivatorValue(selector));
        }

        private static bool ActivatorValue(Match selector)
        {
            return selector.GetActivator().Take(1).ToEnumerable().Single();
        }

        private void BuildVisualTree(Mock<ITemplatedControl> templatedControl)
        {
            templatedControl.Setup(x => x.VisualChildren).Returns(new[]
            {
                new Border
                {
                    TemplatedParent = templatedControl.Object,
                    Content = new TextBlock
                    {
                        TemplatedParent = templatedControl.Object,
                    },
                },
            });
        }
    }
}
