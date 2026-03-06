using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class ImageCompositionTests : TestBase
    {
        private readonly Bitmap _bitmapA;
        private readonly Bitmap _bitmapB;

        public ImageCompositionTests()
            : base(@"Controls\Image\composition")
        {
            _bitmapA = new Bitmap(Path.Combine(OutputPath, "A.png"));
            _bitmapB = new Bitmap(Path.Combine(OutputPath, "B.png"));
        }
        [Fact]
        public async Task Image_Blend_SourceOver() => await TestCompositeMode(BitmapBlendingMode.SourceOver);
        [Fact]
        public async Task Image_Blend_Source() => await TestCompositeMode(BitmapBlendingMode.Source);
        [Fact]
        public async Task Image_Blend_SourceIn() => await TestCompositeMode(BitmapBlendingMode.SourceIn);
        [Fact]
        public async Task Image_Blend_SourceOut() => await TestCompositeMode(BitmapBlendingMode.SourceOut);
        [Fact]
        public async Task Image_Blend_SourceAtop() => await TestCompositeMode(BitmapBlendingMode.SourceAtop);
        [Fact]
        public async Task Image_Blend_Destination() => await TestCompositeMode(BitmapBlendingMode.Destination);
        [Fact]
        public async Task Image_Blend_DestinationIn() => await TestCompositeMode(BitmapBlendingMode.DestinationIn);
        [Fact]
        public async Task Image_Blend_DestinationOut() => await TestCompositeMode(BitmapBlendingMode.DestinationOut);
        [Fact]
        public async Task Image_Blend_DestinationOver() => await TestCompositeMode(BitmapBlendingMode.DestinationOver);
        [Fact]
        public async Task Image_Blend_DestinationAtop() => await TestCompositeMode(BitmapBlendingMode.DestinationAtop);
        [Fact]
        public async Task Image_Blend_Xor() => await TestCompositeMode(BitmapBlendingMode.Xor);
        
        private async Task TestCompositeMode(BitmapBlendingMode blendMode, [CallerMemberName] string testName = "")
        {
            var panel = new Panel();
            panel.Children.Add(new Image() { Source = _bitmapA });
            panel.Children.Add(new Image() { Source = _bitmapB, BlendMode = blendMode });

            var target = new Decorator
            {
                Width = 512,
                Height = 512,
                Child = new Border
                {
                    Background = Brushes.Transparent,
                    Child = panel
                }
            };

            await RenderToFile(target,testName);
            CompareImages(testName);
        }
    }
}
