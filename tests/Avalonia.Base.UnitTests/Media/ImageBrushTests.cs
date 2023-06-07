using Avalonia.Media;
using Avalonia.Media.Imaging;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class ImageBrushTests
    {
        [Fact]
        public void Changing_Source_Raises_Invalidated()
        {
            var bitmap1 = Mock.Of<IImageBrushSource>();
            var bitmap2 = Mock.Of<IImageBrushSource>();
            var target = new ImageBrush(bitmap1);
            
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.Source = bitmap2;
            });
        }
    }
}
