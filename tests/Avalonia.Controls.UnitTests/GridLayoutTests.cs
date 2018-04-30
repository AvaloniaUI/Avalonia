using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Utils;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridLayoutTests
    {
        [Theory]
        [InlineData("100, 200, 300", double.PositiveInfinity, 600d, new[] { 100d, 200d, 300d })]
        [InlineData("100, 200, 300", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("100, 200, 300", 800d, 600d, new[] { 100d, 200d, 300d })]
        [InlineData("100, 200, 300", 600d, 600d, new[] { 100d, 200d, 300d })]
        [InlineData("100, 200, 300", 400d, 400d, new[] { 100d, 200d, 100d })]
        public void MeasureArrange_AllPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,2*,3*", double.PositiveInfinity, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("*,2*,3*", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("*,2*,3*", 600d, 0d, new[] { 100d, 200d, 300d })]
        public void MeasureArrange_AllStarLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("100,2*,3*", 600d, 100d, new[] { 100d, 200d, 300d })]
        public void MeasureArrange_MixStarPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("100,200,Auto", 600d, 300d, new[] { 100d, 200d, 0d })]
        public void MeasureArrange_MixAutoPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,2*,Auto", 600d, 0d, new[] { 200d, 400d, 0d })]
        public void MeasureArrange_MixAutoStarLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,200,Auto", 600d, 200d, new[] { 400d, 200d, 0d })]
        public void MeasureArrange_MixAutoStarPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static void TestRowDefinitionsOnly(string length, double containerLength,
            double expectedDesiredLength, IList<double> expectedLengthList)
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions(length));

            // Measure - Action & Assert
            var measure = layout.Measure(containerLength);
            Assert.Equal(expectedDesiredLength, measure.DesiredLength);
            Assert.Equal(expectedLengthList, measure.LengthList);

            // Arrange - Action & Assert
            var arrange = layout.Arrange(containerLength, measure);
            Assert.Equal(expectedLengthList, arrange.LengthList);
        }
    }
}
