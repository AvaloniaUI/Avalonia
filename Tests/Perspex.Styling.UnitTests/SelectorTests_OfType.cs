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
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = new Selector().OfType<Control1>();

            Assert.True(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = new Selector().OfType<Control1>();

            Assert.False(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void OfType_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = new Selector().OfType<Control1>();

            Assert.True(target.Match(control).ImmediateResult);
        }

        public class Control1 : TestControlBase
        {
        }

        public class Control2 : TestControlBase
        {
        }
    }
}
