// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_OfType
    {
        [Fact]
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = default(Selector).OfType<Control1>();

            Assert.True(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = default(Selector).OfType<Control1>();

            Assert.False(target.Match(control).ImmediateResult);
        }

        [Fact]
        public void OfType_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = default(Selector).OfType<Control1>();

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
