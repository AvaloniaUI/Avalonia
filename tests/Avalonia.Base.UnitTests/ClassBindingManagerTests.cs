using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class ClassBindingManagerTests
    {

        [Fact]
        public void GetClassProperty_Should_Return_Same_Instance_For_Same_Class()
        {
            var property1 = ClassBindingManager.GetClassProperty("Foo");
            var property2 = ClassBindingManager.GetClassProperty("Foo");
            Assert.Same(property1, property2);
        }

        [Fact]
        public void GetClassProperty_Should_Return_Different_Instances_For_Different_Classes()
        {
            var property1 = ClassBindingManager.GetClassProperty("Foo");
            var property2 = ClassBindingManager.GetClassProperty("Bar");
            Assert.NotSame(property1, property2);
        }
    }
}
