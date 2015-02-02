// -----------------------------------------------------------------------
// <copyright file="TemplateExtensionsTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates.UnitTests
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Templates;
    using Perspex.Controls.UnitTests;
    using Xunit;

    public class TemplateExtensionsTests
    {
        /// <summary>
        /// Control templates can themselves contain templated controls. Make sure that
        /// GetTemplateChildren returns only controls that have a TemplatedParent of the
        /// control that is being searched.
        /// </summary>
        [Fact]
        public void GetTemplateChildren_Should_Not_Return_Nested_Template_Controls()
        {
            var target = new TestTemplatedControl();
            var border1 = new Border { Id = "border1", TemplatedParent = target };
            var inner = new TestTemplatedControl { Id = "inner", TemplatedParent = target };
            var border2 = new Border { Id = "border2", TemplatedParent = inner };
            var border3 = new Border { Id = "border3", TemplatedParent = inner };
            var border4 = new Border { Id = "border4", TemplatedParent = target };
            var border5 = new Border { Id = "border5", TemplatedParent = null };

            target.AddVisualChild(border1);
            border1.Content = inner;
            inner.AddVisualChild(border2);
            inner.AddVisualChild(border3);
            border3.Content = border4;
            border4.Content = border5;

            var result = target.GetTemplateChildren().Select(x => x.Id).ToArray();

            Assert.Equal(new[] { "border1", "inner", "border4" }, result);
        }
    }
}