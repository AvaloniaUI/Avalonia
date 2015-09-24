namespace Perspex.Controls.UnitTests.DockPanelTests
{
    using System.Collections.Generic;
    using Layout;
    using Moq;
    using Xunit;

    public class RightDockerTests
    {
        private readonly ILayoutable _layoutable;

        public RightDockerTests()
        {
            var layoutableMock = new Mock<ILayoutable>();
            layoutableMock.Setup(l => l.DesiredSize).Returns(new Size(40, 30));
            _layoutable = layoutableMock.Object;
        }

        [Theory]
        [MemberData("Source")]
        public void Dock(Margins margins, Rect expectedRect)
        {
            var sut = new RightDocker(new Size(100, 50));
            var actualRect = sut.GetDockingRect(_layoutable.DesiredSize, margins, new Alignments(Alignment.Middle, Alignment.Stretch));

            Assert.Equal(expectedRect, actualRect);
        }

        // ReSharper disable once UnusedMember.Global
        public static IEnumerable<object[]> Source => new[]
        {
            new object[] { new Margins(), new Rect(60, 0, 40, 50)},
            new object[] { new Margins { VerticalMargin = new Segment(0, 15) }, new Rect(60, 0, 40, 35)},
            new object[] { new Margins { VerticalMargin = new Segment(15, 0) }, new Rect(60, 15, 40, 35)},
            new object[] { new Margins { VerticalMargin = new Segment(20, 15) }, new Rect(60, 20, 40, 15)},
        };
    }
}