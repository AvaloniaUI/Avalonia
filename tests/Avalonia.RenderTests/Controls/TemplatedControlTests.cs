using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class TemplatedControlTests : TestBase
    {
        public TemplatedControlTests()
            : base(@"Controls\TemplatedControl")
        {
        }

        [Theory,
         InlineData(true),
         InlineData(false)]
        public async Task TemplatedControl_Clips_To_Round_Bounds(bool uniform)
        {
            var cornerRadius = uniform
                ? new CornerRadius(20)
                : new CornerRadius(20, 10, 30, 5);

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new TemplatedControl
                {
                    CornerRadius = cornerRadius,
                    Background = Brushes.LightBlue,
                    ClipToBounds = true,
                    Template = new FuncControlTemplate((_, _) => new Border
                    {
                        Width = 300,
                        Height = 300,
                        Background = Brushes.Red,
                    })
                }
            };

            var testSuffix = nameof(TemplatedControl_Clips_To_Round_Bounds) + "_" + (uniform ? "Uniform" : "NonUniform");
            await RenderToFile(target, testSuffix);
            CompareImages(testSuffix);
        }
    }
}
