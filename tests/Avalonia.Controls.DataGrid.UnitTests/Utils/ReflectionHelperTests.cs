
using Avalonia.Controls.Utils;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Utils
{
    public class ReflectionHelperTests
    {
        [Fact]
        public void SplitPropertyPath_Splits_PropertyPath_With_Cast()
        {
            var path = "(Type).Property";
            var expected = new [] { "Property" };

            var result = TypeHelper.SplitPropertyPath(path);
            
            Assert.Equal(expected, result);
        }
    }
}
