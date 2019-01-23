// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_PropertyEquals
    {
        [Fact]
        public async Task PropertyEquals_Matches_When_Property_Has_Matching_Value()
        {
            var control = new TextBlock();
            var target = default(Selector).PropertyEquals(TextBlock.TextProperty, "foo");
            var activator = target.Match(control).Activator;

            Assert.False(await activator.Take(1));
            control.Text = "foo";
            Assert.True(await activator.Take(1));
            control.Text = null;
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void OfType_PropertyEquals_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new TextBlock();
            var target = default(Selector).OfType<Border>().PropertyEquals(TextBlock.TextProperty, "foo");

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }

        [Fact]
        public void PropertyEquals_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector)
                .OfType<TextBlock>()
                .PropertyEquals(TextBlock.TextProperty, "foo");

            Assert.Equal("TextBlock[Text=foo]", target.ToString());
        }
    }
}
