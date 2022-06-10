using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class GeometryGroupTests
    {
        [Fact]
        public void Children_Should_Have_Initial_Collection()
        {
            var target = new GeometryGroup();

            Assert.NotNull(target.Children);
        }

        [Fact]
        public void Children_Change_Should_Raise_Changed()
        {
            var target = new GeometryGroup();

            var children = new GeometryCollection();

            target.Children = children;

            var isCalled = false;

            target.Changed += (s, e) => isCalled = true;

            children.Add(new StreamGeometry());

            Assert.True(isCalled);
        }
    }
}
