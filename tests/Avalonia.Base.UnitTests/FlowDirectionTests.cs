using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlowDirectionTests
    {
        [Fact]
        public void HasMirrorTransform_Should_Be_True()
        {
            var target = new Visual
            {
                FlowDirection = FlowDirection.RightToLeft,
            };

            Assert.True(target.HasMirrorTransform);    
        }

        [Fact]
        public void HasMirrorTransform_Of_LTR_Children_Should_Be_True_For_RTL_Parent()
        {
            var child = new Visual()
            {
                FlowDirection = FlowDirection.LeftToRight,
            };

            var target = new Visual
            {
                FlowDirection = FlowDirection.RightToLeft,
            };
            target.VisualChildren.Add(child);

            child.InvalidateMirrorTransform();

            Assert.True(target.HasMirrorTransform);
            Assert.True(child.HasMirrorTransform);  
        }

        [Fact]
        public void HasMirrorTransform_Of_Children_Is_Updated_After_Parent_Changed()
        {
            var child = new Visual()
            {
                FlowDirection = FlowDirection.LeftToRight,
            };

            var target = new Decorator
            {
                FlowDirection = FlowDirection.LeftToRight,
            };
            target.VisualChildren.Add(child);

            Assert.False(target.HasMirrorTransform);
            Assert.False(child.HasMirrorTransform);

            target.FlowDirection = FlowDirection.RightToLeft;

            Assert.True(target.HasMirrorTransform);
            Assert.True(child.HasMirrorTransform);
        }
    }
}
