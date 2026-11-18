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
    }
}
