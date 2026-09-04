using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class ContentPresenterTests : TestBase
    {
        public ContentPresenterTests()
            : base(@"Controls\ContentPresenter")
        {
        }

        [Theory,
         InlineData(true),
         InlineData(false)]
        public async Task ContentPresenter_Clips_To_Round_Bounds(bool uniform)
        {
            var cornerRadius = uniform
                ? new CornerRadius(20)
                : new CornerRadius(20, 10, 30, 5);

            var contentPresenter = new ContentPresenter
            {
                CornerRadius = cornerRadius,
                Background = Brushes.LightBlue,
                ClipToBounds = true,
                Content = new Border
                {
                    Width = 300,
                    Height = 300,
                    Background = Brushes.Red,
                }
            };
            var root = new TestRoot(contentPresenter);
            root.ExecuteInitialLayoutPass();
            root.Child = null;

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = contentPresenter
            };

            var testSuffix = nameof(ContentPresenter_Clips_To_Round_Bounds) + "_" + (uniform ? "Uniform" : "NonUniform");
            await RenderToFile(target, testSuffix);
            CompareImages(testSuffix);
        }
    }
}
