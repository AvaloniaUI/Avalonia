// -----------------------------------------------------------------------
// <copyright file="SelectorTests_PropertyEquals.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Perspex.Controls;
    using Perspex.Styling;
    using Xunit;

    public class SelectorTests_PropertyEquals
    {
        [Fact]
        public async Task PropertyEquals_Matches_When_Property_Has_Matching_Value()
        {
            var control = new TextBlock();
            var target = new Selector().PropertyEquals(TextBlock.TextProperty, "foo");
            var activator = target.Match(control).ObservableResult;

            Assert.False(await activator.Take(1));
            control.Text = "foo";
            Assert.True(await activator.Take(1));
            control.Text = null;
            Assert.False(await activator.Take(1));
        }
    }
}
