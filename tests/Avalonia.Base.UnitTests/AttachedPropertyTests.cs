using Avalonia.Controls;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AttachedPropertyTests
    {
        [Fact]
        public void IsAttached_Returns_True()
        {
            var property = new AttachedProperty<string>(
                "Foo",
                typeof(Class1),
                typeof(Control),
                new StyledPropertyMetadata<string>());

            Assert.True(property.IsAttached);
        }

        private class Class1 : AvaloniaObject
        {
        }
    }
}
