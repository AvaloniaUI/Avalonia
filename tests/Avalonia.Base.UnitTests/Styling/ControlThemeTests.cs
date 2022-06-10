using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ControlThemeTests
    {
        [Fact]
        public void ControlTheme_Cannot_Be_Added_To_Styles()
        {
            var target = new ControlTheme(typeof(Button));
            var styles = new Styles();

            Assert.Throws<InvalidOperationException>(() => styles.Add(target));
        }

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

        [Fact]
        public void Style_Without_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style();

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }

        [Fact]
        public void Style_Without_Nesting_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style(x => x.OfType<Button>().Template().OfType<Border>());

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }

        [Fact]
        public void Style_With_NonTemplate_Child_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style(x => x.Nesting().Child().OfType<Border>());

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }

        [Fact]
        public void Style_With_NonTemplate_Descendent_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style(x => x.Nesting().Descendant().OfType<Border>());

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }

        [Fact]
        public void Style_With_NonTemplate_Child_Template_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style(x => x.Nesting().Child().Template().OfType<Border>());

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }

        [Fact]
        public void Style_With_Double_Template_Selector_Cannot_Be_Added_To_Children()
        {
            var target = new ControlTheme(typeof(Button));
            var child = new Style(x => x.Nesting().Template().OfType<ToggleButton>().Template().OfType<Border>());

            Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
        }
    }
}
