using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlowDirectionTests
    {
        [Fact]
        public void HasMirrorTransform_Should_Be_True()
        {
            var target = new Control
            {
                FlowDirection = FlowDirection.RightToLeft,
            };

            Assert.True(target.HasMirrorTransform);    
        }

        [Fact]
        public void HasMirrorTransform_Of_LTR_Children_Should_Be_True_For_RTL_Parent()
        {
            Control child;
            var target = new Decorator
            {
                FlowDirection = FlowDirection.RightToLeft,
                Child = child = new Control()
            };

            child.FlowDirection = FlowDirection.LeftToRight;

            Assert.True(target.HasMirrorTransform);
            Assert.True(child.HasMirrorTransform);  
        }

        [Fact]
        public void HasMirrorTransform_Of_Children_Is_Updated_After_Parent_Changeed()
        {
            Control child;
            var target = new Decorator
            {
                FlowDirection = FlowDirection.LeftToRight,
                Child = child = new Control()
                {
                    FlowDirection = FlowDirection.LeftToRight,
                }
            };

            Assert.False(target.HasMirrorTransform);
            Assert.False(child.HasMirrorTransform);

            target.FlowDirection = FlowDirection.RightToLeft;

            Assert.True(target.HasMirrorTransform);
            Assert.True(child.HasMirrorTransform);
        }
    }
}
