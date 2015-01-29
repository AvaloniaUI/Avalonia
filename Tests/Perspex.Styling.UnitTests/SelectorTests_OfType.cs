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

    public class SelectorTests_OfType
    {
        [Fact]
        public void OfType_Priority_Is_Style()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();

            Assert.Equal(BindingPriority.Style, target.Priority);
        }

        [Fact]
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();

            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = new Selector().OfType<Control1>();

            Assert.False(ActivatorValue(target, control));
        }

        [Fact]
        public void OfType_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().OfType<Control1>();

            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void When_OfType_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>().SubscribeCheck();

            var result = target.GetActivator(control).ToEnumerable().Take(1).ToArray();

            Assert.Equal(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [Fact]
        public void When_OfType_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control2>().SubscribeCheck();

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

        public class Control2 : TestControlBase
        {
        }
    }
}
