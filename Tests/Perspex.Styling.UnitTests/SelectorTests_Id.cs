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

    public class SelectorTests_Id
    {
        [Fact]
        public void Id_Priority_Is_Style()
        {
            var control = new Control1();
            var target = new Selector().Name("foo");

            Assert.Equal(BindingPriority.Style, target.Priority);
        }

        [Fact]
        public async Task Id_Matches_Control_With_Correct_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("foo");
            var activator = target.GetActivator(control);

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Id_Doesnt_Match_Control_Of_Wrong_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("bar");
            var activator = target.GetActivator(control);

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task Id_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().Name("foo");
            var activator = target.GetActivator(control);

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task When_Id_Matches_Control_Other_Selectors_Are_Subscribed()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("foo").SubscribeCheck();

            var result = await target.GetActivator(control).Take(1);

            Assert.Equal(1, control.SubscribeCheckObservable.SubscribedCount);
        }

        [Fact]
        public async Task When_Id_Doesnt_Match_Control_Other_Selectors_Are_Not_Subscribed()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("bar").SubscribeCheck();

            var result = await target.GetActivator(control).Take(1);

            Assert.Equal(0, control.SubscribeCheckObservable.SubscribedCount);
        }

        public class Control1 : TestControlBase
        {
        }
    }
}
