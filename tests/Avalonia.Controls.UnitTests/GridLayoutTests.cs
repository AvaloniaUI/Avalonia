using Avalonia.Controls.Utils;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridLayoutTests
    {
        [Fact]
        public void Measure_AllPixelLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("100,200,300"));

            // Action
            var measure = layout.Measure(800);

            // Assert
            Assert.Equal(measure, new [] { 100d, 200d, 300d });
        }

        [Fact]
        public void Measure_AllStarLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("*,2*,3*"));

            // Action
            var measure = layout.Measure(600);

            // Assert
            Assert.Equal(measure, new [] { 100d, 200d, 300d });
        }

        [Fact]
        public void Measure_MixStarPixelLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("100,2*,3*"));

            // Action
            var measure = layout.Measure(600);

            // Assert
            Assert.Equal(measure, new [] { 100d, 200d, 300d });
        }

        [Fact]
        public void Measure_MixAutoPixelLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("100,200,Auto"));

            // Action
            var measure = layout.Measure(600);

            // Assert
            Assert.Equal(measure, new [] { 100d, 200d, double.PositiveInfinity });
        }

        [Fact]
        public void Measure_MixAutoStarLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("*,2*,Auto"));

            // Action
            var measure = layout.Measure(600);

            // Assert
            Assert.Equal(measure, new[] { 200d, 400d, double.PositiveInfinity });
        }

        [Fact]
        public void Measure_MixAutoStarPixelLength_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("*,200,Auto"));

            // Action
            var measure = layout.Measure(600);

            // Assert
            Assert.Equal(measure, new[] { 400d, 200d, double.PositiveInfinity });
        }

        [Fact]
        public void Measure_AllPixelLengthButNotEnough_Correct()
        {
            // Arrange
            var layout = new GridLayout(new RowDefinitions("100,200,300"));

            // Action
            var measure = layout.Measure(400);

            // Assert
            Assert.Equal(measure, new[] { 100d, 200d, 300d });
        }

        //[Fact]
        //public void Arrange_AllPixelLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("100,200,300"));

        //    // Action
        //    var arrange = layout.Arrange(800);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 100d, 200d, 300d });
        //}

        //[Fact]
        //public void Arrange_AllStarLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("*,2*,3*"));

        //    // Action
        //    var arrange = layout.Arrange(600);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 100d, 200d, 300d });
        //}

        //[Fact]
        //public void Arrange_MixStarPixelLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("100,2*,3*"));

        //    // Action
        //    var arrange = layout.Arrange(600);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 100d, 200d, 300d });
        //}

        //[Fact]
        //public void Arrange_MixAutoPixelLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("100,200,Auto"));

        //    // Action
        //    var arrange = layout.Arrange(600);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 100d, 200d, 300d });
        //}

        //[Fact]
        //public void Arrange_MixAutoStarLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("*,2*,Auto"));

        //    // Action
        //    var arrange = layout.Arrange(600);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 200d, 400d, double.PositiveInfinity });
        //}

        //[Fact]
        //public void Arrange_MixAutoStarPixelLength_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("*,200,Auto"));

        //    // Action
        //    var arrange = layout.Arrange(600);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 400d, 200d, double.PositiveInfinity });
        //}

        //[Fact]
        //public void Arrange_AllPixelLengthButNotEnough_Correct()
        //{
        //    // Arrange
        //    var layout = new GridLayout(new RowDefinitions("100,200,300"));

        //    // Action
        //    var arrange = layout.Arrange(400);

        //    // Assert
        //    Assert.Equal(arrange, new[] { 100d, 200d, 300d });
        //}
    }
}
