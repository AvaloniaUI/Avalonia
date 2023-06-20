using System.Linq;
using Avalonia.Controls.UnitTests;
using Xunit;

namespace Avalonia.Controls.Templates.UnitTests
{
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
            var border1 = new Border
            {
                Name = "border1",
                TemplatedParent = target,
            };
            var inner = new TestTemplatedControl
            {
                Name = "inner",
                TemplatedParent = target,
            };
            var border2 = new Border { Name = "border2", TemplatedParent = inner };
            var border3 = new Border { Name = "border3", TemplatedParent = inner };
            var border4 = new Border { Name = "border4", TemplatedParent = target };
            var border5 = new Border { Name = "border5", TemplatedParent = null };

            target.AddVisualChild(border1);
            border1.Child = inner;
            inner.AddVisualChild(border2);
            inner.AddVisualChild(border3);
            border3.Child = border4;
            border4.Child = border5;

            var result = target.GetTemplateChildren().Select(x => x.Name).ToArray();

            Assert.Equal(new[] { "border1", "inner", "border4" }, result);
        }
    }
}
