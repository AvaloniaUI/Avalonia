using Avalonia.Controls.Mixins;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Mixins
{
    public class PressedMixinTests
    {
        private MouseTestHelper _mouse = new MouseTestHelper();

        [Fact]
        public void Selected_Class_Should_Not_Initially_Be_Added()
        {
            var target = new TestControl();

            Assert.Empty(target.Classes);
        }

        [Fact]
        public void Setting_IsSelected_Should_Add_Selected_Class()
        {
            using var app = UnitTestApplication.Start(new TestServices(threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true)));
            var target = new TestControl();

            _mouse.Down(target);

            Assert.Equal(new[] { ":pressed" }, target.Classes);
        }

        private class TestControl : Control
        {
            static TestControl()
            {
                PressedMixin.Attach<TestControl>();
            }
        }
    }
}
