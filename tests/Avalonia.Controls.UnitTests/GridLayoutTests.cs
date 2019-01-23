using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Utils;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridLayoutTests
    {
        private const double Inf = double.PositiveInfinity;

        [Theory]
        [InlineData("100, 200, 300", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("100, 200, 300", 800d, 600d, new[] { 100d, 200d, 300d })]
        [InlineData("100, 200, 300", 600d, 600d, new[] { 100d, 200d, 300d })]
        [InlineData("100, 200, 300", 400d, 400d, new[] { 100d, 200d, 100d })]
        public void MeasureArrange_AllPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,2*,3*", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("*,2*,3*", 600d, 0d, new[] { 100d, 200d, 300d })]
        public void MeasureArrange_AllStarLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("100,2*,3*", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("100,2*,3*", 600d, 100d, new[] { 100d, 200d, 300d })]
        [InlineData("100,2*,3*", 100d, 100d, new[] { 100d, 0d, 0d })]
        [InlineData("100,2*,3*", 50d, 50d, new[] { 50d, 0d, 0d })]
        public void MeasureArrange_MixStarPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("100,200,Auto", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("100,200,Auto", 600d, 300d, new[] { 100d, 200d, 0d })]
        [InlineData("100,200,Auto", 300d, 300d, new[] { 100d, 200d, 0d })]
        [InlineData("100,200,Auto", 200d, 200d, new[] { 100d, 100d, 0d })]
        [InlineData("100,200,Auto", 100d, 100d, new[] { 100d, 0d, 0d })]
        [InlineData("100,200,Auto", 50d, 50d, new[] { 50d, 0d, 0d })]
        public void MeasureArrange_MixAutoPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,2*,Auto", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("*,2*,Auto", 600d, 0d, new[] { 200d, 400d, 0d })]
        public void MeasureArrange_MixAutoStarLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        [Theory]
        [InlineData("*,200,Auto", 0d, 0d, new[] { 0d, 0d, 0d })]
        [InlineData("*,200,Auto", 600d, 200d, new[] { 400d, 200d, 0d })]
        [InlineData("*,200,Auto", 200d, 200d, new[] { 0d, 200d, 0d })]
        [InlineData("*,200,Auto", 100d, 100d, new[] { 0d, 100d, 0d })]
        public void MeasureArrange_MixAutoStarPixelLength_Correct(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            TestRowDefinitionsOnly(length, containerLength, expectedDesiredLength, expectedLengthList);
        }

        
        /// <summary>
        /// This is needed because Mono somehow converts double array to object array in attribute metadata
        /// </summary>
        static void AssertEqual(IList expected, IReadOnlyList<double> actual)
        {
            var conv = expected.Cast<double>().ToArray();
            Assert.Equal(conv, actual);
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static void TestRowDefinitionsOnly(string length, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions(length));

            // Measure - Action & Assert
            var measure = layout.Measure(containerLength);
            Assert.Equal(expectedDesiredLength, measure.DesiredLength);
            AssertEqual(expectedLengthList, measure.LengthList);

            // Arrange - Action & Assert
            var arrange = layout.Arrange(containerLength, measure);
            AssertEqual(expectedLengthList, arrange.LengthList);
        }

        [Theory]
        [InlineData("100, 200, 300", 600d, new[] { 100d, 200d, 300d }, new[] { 100d, 200d, 300d })]
        [InlineData("*,2*,3*", 0d, new[] { Inf, Inf, Inf }, new[] { 0d, 0d, 0d })]
        [InlineData("100,2*,3*", 100d, new[] { 100d, Inf, Inf }, new[] { 100d, 0d, 0d })]
        [InlineData("100,200,Auto", 300d, new[] { 100d, 200d, 0d }, new[] { 100d, 200d, 0d })]
        [InlineData("*,2*,Auto", 0d, new[] { Inf, Inf, 0d }, new[] { 0d, 0d, 0d })]
        [InlineData("*,200,Auto", 200d, new[] { Inf, 200d, 0d }, new[] { 0d, 200d, 0d })]
        public void MeasureArrange_InfiniteMeasure_Correct(string length, double expectedDesiredLength,
            IList expectedMeasureList, IList expectedArrangeList)
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions(length));

            // Measure - Action & Assert
            var measure = layout.Measure(Inf);
            Assert.Equal(expectedDesiredLength, measure.DesiredLength);
            AssertEqual(expectedMeasureList, measure.LengthList);

            // Arrange - Action & Assert
            var arrange = layout.Arrange(measure.DesiredLength, measure);
            AssertEqual(expectedArrangeList, arrange.LengthList);
        }

        [Theory]
        [InlineData("Auto,*,*", new[] { 100d, 100d, 100d }, 600d, 300d, new[] { 100d, 250d, 250d })]
        public void MeasureArrange_ChildHasSize_Correct(string length,
            IList childLengthList, double containerLength,
            double expectedDesiredLength, IList expectedLengthList)
        {
            // Arrange
            var lengthList = new ColumnDefinitions(length);
            var layout = new GridLayout(lengthList);
            layout.AppendMeasureConventions(
                Enumerable.Range(0, lengthList.Count).ToDictionary(x => x, x => (x, 1)),
                x => (double)childLengthList[x]);

            // Measure - Action & Assert
            var measure = layout.Measure(containerLength);
            Assert.Equal(expectedDesiredLength, measure.DesiredLength);
            AssertEqual(expectedLengthList, measure.LengthList);

            // Arrange - Action & Assert
            var arrange = layout.Arrange(containerLength, measure);
            AssertEqual(expectedLengthList, arrange.LengthList);
        }

        [Theory]
        [InlineData(Inf, 250d, new[] { 100d, Inf, Inf }, new[] { 100d, 50d, 100d })]
        [InlineData(400d, 250d, new[] { 100d, 100d, 200d }, new[] { 100d, 100d, 200d })]
        [InlineData(325d, 250d, new[] { 100d, 75d, 150d }, new[] { 100d, 75d, 150d })]
        [InlineData(250d, 250d, new[] { 100d, 50d, 100d }, new[] { 100d, 50d, 100d })]
        [InlineData(160d, 160d, new[] { 100d, 20d, 40d }, new[] { 100d, 20d, 40d })]
        public void MeasureArrange_ChildHasSizeAndHasMultiSpan_Correct(
            double containerLength, double expectedDesiredLength,
            IList expectedMeasureLengthList, IList expectedArrangeLengthList)
        {
            var length = "100,*,2*";
            var childLengthList = new[] { 150d, 150d, 150d };
            var spans = new[] { 1, 2, 1 };

            // Arrange
            var lengthList = new ColumnDefinitions(length);
            var layout = new GridLayout(lengthList);
            layout.AppendMeasureConventions(
                Enumerable.Range(0, lengthList.Count).ToDictionary(x => x, x => (x, spans[x])),
                x => childLengthList[x]);

            // Measure - Action & Assert
            var measure = layout.Measure(containerLength);
            Assert.Equal(expectedDesiredLength, measure.DesiredLength);
            AssertEqual(expectedMeasureLengthList, measure.LengthList);

            // Arrange - Action & Assert
            var arrange = layout.Arrange(
                double.IsInfinity(containerLength) ? measure.DesiredLength : containerLength,
                measure);
            AssertEqual(expectedArrangeLengthList, arrange.LengthList);
        }
    }
}
