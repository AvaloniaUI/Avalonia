using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class MenuItemTests
    {
        [Fact]
        public void Header_Of_Minus_Should_Apply_Separator_Pseudoclass()
        {
            var target = new MenuItem { Header = "-" };

            Assert.True(target.Classes.Contains(":separator"));
        }

        [Fact]
        public void Separator_Item_Should_Set_Focusable_False()
        {
            var target = new MenuItem { Header = "-" };

            Assert.False(target.Focusable);
        }
    }
}
