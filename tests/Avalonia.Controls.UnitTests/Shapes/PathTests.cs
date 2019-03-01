using Avalonia.Controls.Shapes;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes
{
    public class PathTests
    {
        [Fact]
        public void Path_With_Null_Data_Does_Not_Throw_On_Measure()
        {
            var target = new Path();

            target.Measure(Size.Infinity);
        }
    }
}
