using System.Globalization;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class MatrixTests
    {
        [Fact]
        public void Parse_Parses()
        {
            var matrix = Matrix.Parse("1,2,3,-4,5 6", CultureInfo.CurrentCulture);
            var expected = new Matrix(1, 2, 3, -4, 5, 6);
            Assert.Equal(expected, matrix);
        }
    }
}