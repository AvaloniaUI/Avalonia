using Avalonia.Data;
using Avalonia.Controls;
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

        [Fact]
        public void SetClass_Should_Add_Class()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClass(target, "Foo", true);

            Assert.Contains("Foo", target.Classes);
        }

        [Fact]
        public void SetClass_Should_Remove_Added_Class()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClass(target, "Foo", true);
            ClassBindingManager.SetClass(target, "Foo", false);

            Assert.DoesNotContain("Foo", target.Classes);
        }

        [Fact]
        public void SetClasses_Should_Add_Classes()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClasses(target, "Foo Bar");

            Assert.Contains("Foo", target.Classes);
            Assert.Contains("Bar", target.Classes);
        }

        [Fact]
        public void SetClasses_Should_Remove_Added_Classes()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClasses(target, "Foo Bar");
            ClassBindingManager.SetClasses(target, "");

            Assert.Empty(target.Classes);
        }

        [Fact]
        public void SetClasses_Should_Keep_PseudoClasses()
        {
            var target = new StyledElement();
            ((IPseudoClasses)target.Classes).Add(":Baz");

            ClassBindingManager.SetClasses(target, "Foo");

            Assert.Equal(new[] { ":Baz", "Foo" }, target.Classes);
        }

        [Fact]
        public void SetClass_Should_Override_SetClasses_Adding()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClasses(target, "Foo Bar");
            ClassBindingManager.SetClass(target, "Foo", false);

            Assert.Contains("Bar", target.Classes);
            Assert.DoesNotContain("Foo", target.Classes);
        }

        [Fact]
        public void SetClass_Should_Override_SetClasses_Adding_When_Set_Before()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClass(target, "Foo", false);
            ClassBindingManager.SetClasses(target, "Foo Bar");

            Assert.Contains("Bar", target.Classes);
            Assert.DoesNotContain("Foo", target.Classes);
        }

        [Fact]
        public void SetClass_Should_Override_SetClasses_Removing()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClasses(target, "Bar");
            ClassBindingManager.SetClass(target, "Foo", true);

            Assert.Contains("Foo", target.Classes);
            Assert.Contains("Bar", target.Classes);
        }

        [Fact]
        public void SetClass_Should_Override_SetClasses_Removing_When_Set_Before()
        {
            var target = new StyledElement();

            ClassBindingManager.SetClass(target, "Foo", true);
            ClassBindingManager.SetClasses(target, "Bar");

            Assert.Contains("Foo", target.Classes);
            Assert.Contains("Bar", target.Classes);
        }

        [Fact]
        public void BindClass_Should_Update_Classes()
        {
            var target = new StyledElement();

            using var d = ClassBindingManager.BindClass(target, "Bar", new Binding { Source = true }, null);

            Assert.Contains("Bar", target.Classes);
        }

        [Fact]
        public void BindClasses_Should_Update_Classes()
        {
            var target = new StyledElement();

            using var d = ClassBindingManager.BindClasses(target, new Binding { Source = "Foo Bar" }, null);

            Assert.Equal("Foo Bar", ClassBindingManager.GetClasses(target));
            Assert.Contains("Foo", target.Classes);
            Assert.Contains("Bar", target.Classes);
        }

        [Fact]
        public void IsClassesBindingProperty_Should_Detect_Classes_Properties()
        {
            var prop = ClassBindingManager.GetClassProperty("Foo");

            var result = ClassBindingManager.IsClassesBindingProperty(prop, out var name);

            Assert.True(result);
            Assert.Equal("Foo", name);
        }
    }
}
