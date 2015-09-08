// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Xunit;

namespace Perspex.Styling.UnitTests
{
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
