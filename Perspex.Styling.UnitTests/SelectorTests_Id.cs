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
    using Xunit;

    public class SelectorTests_Id
    {
        [Fact]
        public void Id_Priority_Is_Style()
        {
            var control = new Control1();
            var target = new Selector().Id("foo");

            Assert.Equal(BindingPriority.Style, target.Priority);
        }

        [Fact]
        public void Id_Matches_Control_With_Correct_Id()
        {
            var control = new Control1 { Id = "foo" };
            var target = new Selector().Id("foo");

            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void Id_Doesnt_Match_Control_Of_Wrong_Id()
        {
            var control = new Control1 { Id = "foo" };
            var target = new Selector().Id("bar");

            Assert.False(ActivatorValue(target, control));
        }

        [Fact]
        public void Id_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().Id("foo");

            Assert.False(ActivatorValue(target, control));
        }

        [Fact]
        public void When_Id_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1 { Id = "foo" };
            var target = new Selector().Id("foo").SubscribeCheck();

            var result = target.GetActivator(control).ToEnumerable().Take(1).ToArray();

            Assert.Equal(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [Fact]
        public void When_Id_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1 { Id = "foo" };
            var target = new Selector().Id("bar").SubscribeCheck();

            var result = target.GetActivator(control).ToEnumerable().Take(1).ToArray();

            Assert.Equal(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        private static bool ActivatorValue(Selector selector, IStyleable control)
        {
            return selector.GetActivator(control).Take(1).ToEnumerable().Single();
        }

        public class Control1 : TestControlBase
        {
        }
    }
}
