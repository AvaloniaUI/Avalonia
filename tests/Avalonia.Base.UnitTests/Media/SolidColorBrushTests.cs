using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class SolidColorBrushTests
    {
        [Fact]
        public void Changing_Color_Raises_Invalidated()
        {
            var target = new SolidColorBrush(Colors.Red);
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.Color = Colors.Green;
            });
        }
    }
}
