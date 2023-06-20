using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Template
    {
        [Fact]
        public void Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new TestTemplatedControl();
            var border = (Border)target.GetVisualChildren().Single();
            var selector = default(Selector)
                .OfType(target.GetType())
                .Template()
                .OfType<Border>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Control_Not_In_Template_Is_Not_Matched_With_Template_Selector()
        {
            var target = new TestTemplatedControl();
            var border = (Border)target.GetVisualChildren().Single();
            var selector = default(Selector)
                .OfType(target.GetType())
                .Template()
                .OfType<Border>();

            border.TemplatedParent = null;

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Control_In_Template_Of_Wrong_Type_Is_Not_Matched_With_Template_Selector()
        {
            var target = new TestTemplatedControl();
            var border = (Border)target.GetVisualChildren().Single();
            var selector = default(Selector)
                .OfType<Button>()
                .Template()
                .OfType<Border>();

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Nested_Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new TestTemplatedControl();
            var textBlock = (TextBlock)target.VisualChildren.Single().VisualChildren.Single();
            var selector = default(Selector)
                .OfType(target.GetType())
                .Template()
                .OfType<TextBlock>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(textBlock).Result);
        }

        [Fact]
        public void Control_In_Template_Is_Matched_With_TypeOf_TemplatedControl()
        {
            var target = new TestTemplatedControl();
            var styleKey = typeof(TestTemplatedControl);
            var border = (Border)target.VisualChildren.Single();
            var selector = default(Selector).OfType(styleKey).Template().OfType<Border>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public async Task Control_In_Template_Is_Matched_With_Correct_TypeOf_And_Class_Of_TemplatedControl()
        {
            var target = new TestTemplatedControl { Classes = { "foo" } };
            var styleKey = typeof(TestTemplatedControl);

            var border = (Border)target.VisualChildren.Single();
            var selector = default(Selector).OfType(styleKey).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Control_In_Template_Is_Not_Matched_With_Correct_TypeOf_And_Wrong_Class_Of_TemplatedControl()
        {
            var target = new TestTemplatedControl { Classes = { "bar" } };

            var border = (Border)target.VisualChildren.Single();
            var selector = default(Selector).OfType(typeof(TestTemplatedControl)).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Nested_Selector_Is_Unsubscribed()
        {
            var target = new TestTemplatedControl { Classes = { "foo" } };
            var border = (Border)target.VisualChildren.Single();
            var selector = default(Selector).OfType(typeof(TestTemplatedControl)).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;

            using (activator.Subscribe(_ => { }))
            {
                Assert.Equal(1, target.Classes.ListenerCount);
            }

            Assert.Equal(0, target.Classes.ListenerCount);
        }

        private class TestTemplatedControl : Controls.Primitives.TemplatedControl
        {
            public TestTemplatedControl()
            {
                VisualChildren.Add(new Border
                {
                    TemplatedParent = this,
                    Child = new TextBlock
                    {
                        TemplatedParent = this,
                    },
                });
            }
        }
    }
}
