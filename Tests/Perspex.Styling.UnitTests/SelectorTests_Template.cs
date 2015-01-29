// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System.Linq;
    using System.Reactive.Linq;
    using Moq;
    using Perspex.Controls;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Xunit;

    public class SelectorTests_Template
    {
        [Fact]
        public void Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            this.BuildVisualTree(target);

            var border = (Border)target.Object.GetVisualChildren().Single();

            var selector = new Selector().Template().OfType<Border>();

            Assert.True(ActivatorValue(selector, border));
        }

        [Fact]
        public void Nested_Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            this.BuildVisualTree(target);

            var textBlock = (TextBlock)target.Object.VisualChildren.Single().VisualChildren.Single();

            var selector = new Selector().Template().OfType<TextBlock>();

            Assert.True(ActivatorValue(selector, textBlock));
        }

        [Fact]
        public void Control_In_Template_Is_Matched_With_TypeOf_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            this.BuildVisualTree(target);

            var border = (Border)target.Object.VisualChildren.Single();

            var selector = new Selector().OfType(templatedControl.Object.GetType()).Template().OfType<Border>();

            Assert.True(ActivatorValue(selector, border));
        }

        [Fact]
        public void Control_In_Template_Is_Matched_With_Correct_TypeOf_And_Class_Of_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            this.BuildVisualTree(target);

            styleable.Setup(x => x.Classes).Returns(new Classes("foo"));
            var border = (Border)target.Object.VisualChildren.Single();

            var selector = new Selector().OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();

            Assert.True(ActivatorValue(selector, border));
        }

        [Fact]
        public void Control_In_Template_Is_Not_Matched_With_Correct_TypeOf_And_Wrong_Class_Of_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            this.BuildVisualTree(target);

            styleable.Setup(x => x.Classes).Returns(new Classes("bar"));
            var border = (Border)target.Object.VisualChildren.Single();

            var selector = new Selector().OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();

            Assert.False(ActivatorValue(selector, border));
        }

        private static bool ActivatorValue(Selector selector, IStyleable control)
        {
            return selector.GetActivator(control).Take(1).ToEnumerable().Single();
        }

        private void BuildVisualTree<T>(Mock<T> templatedControl) where T : class, IVisual
        {
            templatedControl.Setup(x => x.VisualChildren).Returns(new Controls
            {
                new Border
                {
                    [Border.TemplatedParentProperty] = templatedControl.Object,
                    Content = new TextBlock
                    {
                        [Border.TemplatedParentProperty] = templatedControl.Object,
                    },
                },
            });
        }
    }
}
