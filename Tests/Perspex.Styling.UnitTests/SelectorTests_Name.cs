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

    public class SelectorTests_Name
    {
        [Fact]
        public void Name_Matches_Control_With_Correct_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("foo");

            Assert.True(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void Name_Doesnt_Match_Control_Of_Wrong_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = new Selector().Name("bar");

            Assert.False(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void Name_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().Name("foo");
            var activator = target.Match(control);

            Assert.False(target.Match(control).ImmediateResult);
        }

        public class Control1 : TestControlBase
        {
        }
    }
}
