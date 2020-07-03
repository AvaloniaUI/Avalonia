using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class InputElement_Focus
    {
        [Fact]
        public void Focus_Should_Set_FocusManager_Current()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();

                Assert.Same(target, FocusManager.Instance.Current);
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Removed_From_VisualTree()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                root.Child = null;

                Assert.Null(FocusManager.Instance.Current);
            }
        }

        [Fact]
        public void Focus_Pseudoclass_Should_Be_Applied_On_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator();
                var target2 = new Decorator();
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();


                FocusManager.Instance?.Focus(target1);
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus"));

                FocusManager.Instance?.Focus(target2, NavigationMethod.Tab);
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus"));
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus"));
            }
        }

        [Fact]
        public void Control_FocusVsisible_Pseudoclass_Should_Be_Applied_On_Tab_And_DirectionalFocus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target1 = new Decorator();
                var target2 = new Decorator();
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            target1,
                            target2
                        }
                    }
                };

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                FocusManager.Instance?.Focus(target1);
                Assert.True(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-visible"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus-visible"));

                FocusManager.Instance?.Focus(target2, NavigationMethod.Tab);
                Assert.False(target1.IsFocused);
                Assert.False(target1.Classes.Contains(":focus-visible"));
                Assert.True(target2.IsFocused);
                Assert.True(target2.Classes.Contains(":focus-visible"));

                FocusManager.Instance?.Focus(target1, NavigationMethod.Directional);
                Assert.True(target1.IsFocused);
                Assert.True(target1.Classes.Contains(":focus-visible"));
                Assert.False(target2.IsFocused);
                Assert.False(target2.Classes.Contains(":focus-visible"));
            }
        }
    }
}
