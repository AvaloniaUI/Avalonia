using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class StyledPropertyTests
    {
        [Fact]
        public void AddOwnered_Property_Should_Equal_Original()
        {
            var p1 = new StyledProperty<string>(
                "p1", 
                typeof(Class1),
                typeof(Class1),
                new StyledPropertyMetadata<string>());
            var p2 = p1.AddOwner<Class2>();

            Assert.Equal(p1, p2);
            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
            Assert.True(p1 == p2);
        }

        [Fact]
        public void AddOwnered_Property_Should_Be_Same()
        {
            var p1 = new StyledProperty<string>(
                "p1",
                typeof(Class1),
                typeof(Class1),
                new StyledPropertyMetadata<string>());
            var p2 = p1.AddOwner<Class2>();

            Assert.Same(p1, p2);
        }

        private class Class1 : AvaloniaObject
        {
        }

        private class Class2 : AvaloniaObject
        {
        }
    }
}
