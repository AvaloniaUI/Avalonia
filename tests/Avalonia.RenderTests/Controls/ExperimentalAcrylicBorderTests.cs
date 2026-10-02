using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class ExperimentalAcrylicBorderTests : TestBase
    {
        public ExperimentalAcrylicBorderTests()
            : base(@"Controls\ExperimentalAcrylicBorder")
        {
        }

        [Theory,
         InlineData(true),
         InlineData(false)]
        public async Task ExperimentalAcrylicBorder_Clips_To_Round_Bounds(bool uniform)
        {
            var cornerRadius = uniform
                ? new CornerRadius(20)
                : new CornerRadius(20, 10, 30, 5);

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new ExperimentalAcrylicBorder
                {
                    CornerRadius = cornerRadius,
                    ClipToBounds = true,
                    Child = new Border
                    {
                        Width = 300,
                        Height = 300,
                        Background = Brushes.Red,
                    }
                }
            };

            var testSuffix = nameof(ExperimentalAcrylicBorder_Clips_To_Round_Bounds) + "_" + (uniform ? "Uniform" : "NonUniform");
            await RenderToFile(target, testSuffix);
            CompareImages(testSuffix);
        }
    }
}
