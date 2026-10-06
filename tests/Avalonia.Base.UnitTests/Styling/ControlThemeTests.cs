using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.PropertyStore;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ControlThemeTests
    {
        [Fact]
        public void ControlTheme_Attached_As_Theme_And_TemplatedParentTheme_Creates_Distinct_Instances()
        {
            // The same ControlTheme can legitimately be attached to a control twice:
            // once as its own theme and once via the templated parent's theme. Each
            // attach must produce a distinct StyleInstance so that the same object is
            // never inserted twice into the same ValueStore.
            var theme = new ControlTheme(typeof(Button));
            theme.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));

            var target = new Button();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target, FrameType.Theme));
            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target, FrameType.TemplatedParentTheme));

            var instances = target.GetValueStore().Frames.OfType<StyleInstance>().ToList();
            Assert.Equal(2, instances.Count);
            Assert.NotSame(instances[0], instances[1]);
        }

        [Fact]
        public void ControlTheme_Reattach_After_RemoveFrames_Does_Not_Duplicate_Instance()
        {
            var theme = new ControlTheme(typeof(Button));
            theme.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));

            var target = new Button();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target, FrameType.Theme));

            var store = target.GetValueStore();
            store.BeginStyling();
            try
            {
                store.RemoveFrames(FrameType.Theme);
            }
            finally
            {
                store.EndStyling();
            }

            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target, FrameType.Theme));

            var instances = store.Frames.OfType<StyleInstance>().ToList();
            Assert.Equal(1, instances.Count);
        }

        [Fact]
        public void ControlTheme_Shares_Instance_Between_Controls_For_Same_FrameType()
        {
            var theme = new ControlTheme(typeof(Button));
            theme.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));

            var target1 = new Button();
            var target2 = new Button();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target1, FrameType.Theme));
            Assert.Equal(SelectorMatchResult.AlwaysThisType, theme.TryAttach(target2, FrameType.Theme));

            var instance1 = target1.GetValueStore().Frames.OfType<StyleInstance>().Single();
            var instance2 = target2.GetValueStore().Frames.OfType<StyleInstance>().Single();
            Assert.Same(instance1, instance2);
        }

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
