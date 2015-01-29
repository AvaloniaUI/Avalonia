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
    using Perspex.Styling;

    ////[TestFixture]
    ////public class SelectorTests_Template
    ////{
    ////    [Fact]
    ////    public void Control_In_Template_Is_Matched_With_Template_Selector()
    ////    {
    ////        var templatedControl = new Mock<ITemplatedControl>();
    ////        var styleable = templatedControl.As<IStyleable>();
    ////        this.BuildVisualTree(templatedControl);

    ////        var border = (Border)templatedControl.Object.VisualChildren.Single();

    ////        var selector = new Selector().Template().OfType<Border>();

    ////        Assert.True(ActivatorValue(selector, border));
    ////    }

    ////    [Fact]
    ////    public void Nested_Control_In_Template_Is_Matched_With_Template_Selector()
    ////    {
    ////        var templatedControl = new Mock<ITemplatedControl>();
    ////        var styleable = templatedControl.As<IStyleable>();
    ////        this.BuildVisualTree(templatedControl);

    ////        var textBlock = (TextBlock)templatedControl.Object.VisualChildren.Single().VisualChildren.Single();

    ////        var selector = new Selector().Template().OfType<TextBlock>();

    ////        Assert.True(ActivatorValue(selector, textBlock));
    ////    }

    ////    [Fact]
    ////    public void Control_In_Template_Is_Matched_With_TypeOf_TemplatedControl()
    ////    {
    ////        var templatedControl = new Mock<TestTemplatedControl>();
    ////        this.BuildVisualTree(templatedControl);

    ////        var border = (Border)templatedControl.Object.VisualChildren.Single();

    ////        var selector = new Selector().OfType(templatedControl.Object.GetType()).Template().OfType<Border>();

    ////        Assert.True(ActivatorValue(selector,border));
    ////    }

    ////    [Fact]
    ////    public void Control_In_Template_Is_Matched_With_Correct_TypeOf_And_Class_Of_TemplatedControl()
    ////    {
    ////        var templatedControl = new Mock<TestTemplatedControl>();
    ////        this.BuildVisualTree(templatedControl);

    ////        templatedControl.Setup(x => x.Classes).Returns(new Classes("foo"));
    ////        var border = (Border)templatedControl.Object.VisualChildren.Single();

    ////        var selector = new Selector().OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();

    ////        Assert.True(ActivatorValue(selector, border));
    ////    }

    ////    [Fact]
    ////    public void Control_In_Template_Is_Not_Matched_With_Correct_TypeOf_And_Wrong_Class_Of_TemplatedControl()
    ////    {
    ////        var templatedControl = new Mock<TestTemplatedControl>();
    ////        this.BuildVisualTree(templatedControl);

    ////        templatedControl.Setup(x => x.Classes).Returns(new Classes("bar"));
    ////        var border = (Border)templatedControl.Object.VisualChildren.Single();

    ////        var selector = new Selector().OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();

    ////        Assert.False(ActivatorValue(selector, border));
    ////    }

    ////    private static bool ActivatorValue(Selector selector, IStyleable control)
    ////    {
    ////        return selector.GetActivator(control).Take(1).ToEnumerable().Single();
    ////    }

    ////    private void BuildVisualTree<T>(Mock<T> templatedControl) where T : class, ITemplatedControl
    ////    {
    ////        templatedControl.Setup(x => x.VisualChildren).Returns(new[]
    ////        {
    ////            new Border
    ////            {
    ////                TemplatedParent = templatedControl.Object,
    ////                Content = new TextBlock
    ////                {
    ////                    TemplatedParent = templatedControl.Object,
    ////                },
    ////            },
    ////        });
    ////    }
    ////}
}
