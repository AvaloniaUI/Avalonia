using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SliderTests : ScopedTestBase
    {
        [Theory]
        [InlineData("PART_DecreaseButton")]
        [InlineData("PART_IncreaseButton")]
        public void Tapping_The_Track_Raises_Tapped(string part)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var slider = new Slider { Width = 200, Height = 30, Minimum = 0, Maximum = 100, Value = 50 };
                var window = new Window { Width = 300, Height = 100, Content = slider };

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass();

                var button = slider.GetVisualDescendants().OfType<RepeatButton>().First(b => b.Name == part);
                var tapped = 0;
                slider.AddHandler(InputElement.TappedEvent, (_, _) => tapped++);

                var mouse = new MouseTestHelper();
                var point = new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);

                mouse.Down(button, position: point);
                window.LayoutManager.ExecuteLayoutPass();
                mouse.Up(button, position: point);

                Assert.Equal(1, tapped);
            }
        }
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
