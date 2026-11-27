using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class ColorSpectrumTests : ScopedTestBase
    {
        [Fact]
        public void Color_Set_Through_HsvColor_Is_Kept()
        {
            var target = new ColorSpectrum();

            for (var i = 0; i <= 255; i++)
            {
                var color = Color.FromArgb((byte)i, (byte)i, (byte)i, (byte)i);

                target.HsvColor = color.ToHsv();

                Assert.Equal(color, target.Color);
            }
        }

        [Fact]
        public void Color_Matches_HsvColor_ToRgb()
        {
            var target = new ColorSpectrum();
            var hsvColor = HsvColor.FromAhsv(0.5, 200, 0.5, 0.5);

            target.HsvColor = hsvColor;

            Assert.Equal(hsvColor.ToRgb(), target.Color);
        }
    }
}
