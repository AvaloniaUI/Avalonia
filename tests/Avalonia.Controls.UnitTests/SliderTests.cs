using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SliderTests
    {
        [Fact]
        public void Default_Orientation_Should_Be_Horizontal()
        {
            var slider = new Slider();
            Assert.Equal(Orientation.Horizontal, slider.Orientation);
        }

        [Fact]
        public void Should_Set_Horizontal_Class()
        {
            var slider = new Slider
            {
                Orientation = Orientation.Horizontal
            };

            Assert.Contains(slider.Classes, ":horizontal".Equals);
        }

        [Fact]
        public void Should_Set_Vertical_Class()
        {
            var slider = new Slider
            {
                Orientation = Orientation.Vertical
            };

            Assert.Contains(slider.Classes, ":vertical".Equals);
        }
    }
}
