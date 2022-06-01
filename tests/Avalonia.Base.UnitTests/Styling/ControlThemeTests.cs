using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ControlThemeTests
    {
        [Fact]
        public void ControlTheme_Cannot_Be_Added_To_Style_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var style = new Style();

            Assert.Throws<InvalidOperationException>(() => style.Children.Add(target));
        }

        [Fact]
        public void ControlTheme_Cannot_Be_Added_To_ControlTheme_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var other = new ControlTheme(typeof(CheckBox));

            Assert.Throws<InvalidOperationException>(() => other.Children.Add(target));
        }
    }
}
