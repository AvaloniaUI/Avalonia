namespace Perspex.Controls.UnitTests.DockPanelTests
{
    using System.Collections.Generic;
    using Layout;
    using Moq;
    using Xunit;

    public class LeftDockerTests
    {
        private readonly ILayoutable _layoutable;

        public LeftDockerTests()
        {
            var layoutableMock = new Mock<ILayoutable>();
            layoutableMock.Setup(l => l.DesiredSize).Returns(new Size(40, 30));
            _layoutable = layoutableMock.Object;
        }

        [Theory]
        [MemberData("Source")]
        public void Dock(Margins margins, Rect expectedRect)
        {
            var sut = new LeftDocker(new Size(100, 50));
            var actualRect = sut.GetDockingRect(_layoutable.DesiredSize, margins, new Alignments(Alignment.Middle, Alignment.Stretch));

            Assert.Equal(expectedRect, actualRect);
        }

        // ReSharper disable once UnusedMember.Global
        public static IEnumerable<object[]> Source => new[]
        {
            new object[] { new Margins(), new Rect(0, 0, 40, 50)},
            new object[] { new Margins { VerticalMargin = new Segment(15, 0) }, new Rect(0, 15, 40, 35)},
            new object[] { new Margins { VerticalMargin = new Segment(0, 15) }, new Rect(0, 0, 40, 35)},
            new object[] { new Margins { VerticalMargin = new Segment(20, 15) }, new Rect(0, 20, 40, 15)},
        };
    }
}