// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
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
        public async Task OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();
            var activator = target.GetActivator(control);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = new Selector().OfType<Control1>();
            var activator = target.GetActivator(control);

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task OfType_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().OfType<Control1>();
            var activator = target.GetActivator(control);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task When_OfType_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>().SubscribeCheck();

            var result = await target.GetActivator(control).Take(1);

            Assert.Equal(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [Fact]
        public async Task When_OfType_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control2>().SubscribeCheck();

            var result = await target.GetActivator(control).Take(1);

            Assert.Equal(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        public class Control1 : TestControlBase
        {
        }

        public class Control2 : TestControlBase
        {
        }
    }
}
