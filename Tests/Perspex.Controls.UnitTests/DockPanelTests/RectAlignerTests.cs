namespace Perspex.Controls.UnitTests.DockPanelTests
{
    using Layout;
    using Xunit;

    public class RectAlignerTests
    {
        private readonly Rect _container = new Rect(0, 0, 40, 40);
        private readonly Size _child = new Size(20, 20);

        [Theory]
        [MemberData("TestData")]
        public void LefTopTest(Alignment horz, Alignment vert, Rect expectedRect)
        {
            var actualRect = _container.AlignChild(_child, horz, vert);
            Assert.Equal(expectedRect, actualRect);
        }

        // ReSharper disable once UnusedMember.Global
        public static object[] TestData => new object[]
        {
            new object[] {Alignment.Start, Alignment.Start, new Rect(0, 0, 20, 20)},
            new object[] {Alignment.Middle, Alignment.Start, new Rect(10, 0, 20, 20)},
            new object[] {Alignment.End, Alignment.Start, new Rect(20, 0, 20, 20)},
            new object[] {Alignment.Stretch, Alignment.Start, new Rect(0, 0, 40, 20)},

            new object[] {Alignment.Start, Alignment.Middle, new Rect(0, 10, 20, 20)},
            new object[] {Alignment.Middle, Alignment.Middle, new Rect(10, 10, 20, 20)},
            new object[] {Alignment.End, Alignment.Middle, new Rect(20, 10, 20, 20)},
            new object[] {Alignment.Stretch, Alignment.Middle, new Rect(0, 10, 40, 20)},

            new object[] {Alignment.Start, VerticalAlignment.Bottom, new Rect(0, 20, 20, 20)},
            new object[] {Alignment.Middle, VerticalAlignment.Bottom, new Rect(10, 20, 20, 20)},
            new object[] {Alignment.End, VerticalAlignment.Bottom, new Rect(20, 20, 20, 20)},
            new object[] {Alignment.Stretch, VerticalAlignment.Bottom, new Rect(0, 20, 40, 20)},

            new object[] {Alignment.Stretch, VerticalAlignment.Stretch, new Rect(0, 0, 40, 40)},
        };
    }
}