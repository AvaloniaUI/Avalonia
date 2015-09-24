namespace Perspex.Controls.UnitTests.DockPanelTests
{
    using Xunit;


    public class AlignerTests
    {
        [Fact]
        public void ToStartTest()
        {
            Segment container = new Segment(2, 5);

            var aligned = container.AlignToStart(2);
            Assert.Equal(new Segment(2, 4), aligned);
        }

        [Fact]
        public void ToEndTest()
        {
            Segment container = new Segment(2, 5);

            var aligned = container.AlignToEnd(2);
            Assert.Equal(new Segment(3, 5), aligned);
        }

        [Fact]
        public void ToMiddleTest()
        {
            Segment container = new Segment(2, 5);

            var aligned = container.AlignToMiddle(2);
            Assert.Equal(new Segment(2.5, 4.5), aligned);
        }

        [Fact]
        public void ToMiddleTest2()
        {
            Segment container = new Segment(0, 500);

            var aligned = container.AlignToMiddle(200);
            Assert.Equal(new Segment(150, 350), aligned);
        }
    }
}