using System.Collections.Specialized;
using System.Linq;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DecoratorTests
    {
        [Fact]
        public void Setting_Content_Should_Set_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;

            Assert.Equal(child.Parent, decorator);
            Assert.Equal(((ILogical)child).LogicalParent, decorator);
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;
            decorator.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Content_Control_Should_Appear_In_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;

            Assert.Equal(new[] { child }, ((ILogical)decorator).GetLogicalChildren().ToList());
        }

        [Fact]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;
            decorator.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)decorator).GetLogicalChildren().ToList());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildrenChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = 0;

            ((ILogical)decorator).LogicalChildrenChanged += (s, e) => ++called;

            decorator.Child = child;

            Assert.Equal(1, called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildrenChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = 0;

            decorator.Child = child;

            ((ILogical)decorator).LogicalChildrenChanged += (s, e) => ++called;

            decorator.Child = null;

            Assert.Equal(1, called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildrenChanged()
        {
            var decorator = new Decorator();
            var child1 = new Control();
            var child2 = new Control();
            var called = 0;

            decorator.Child = child1;

            ((ILogical)decorator).LogicalChildrenChanged += (s, e) => ++called;

            decorator.Child = child2;

            Assert.Equal(1, called);
        }

        [Fact]
        public void Measure_Should_Return_Padding_When_No_Child_Present()
        {
            var target = new Decorator
            {
                Padding = new Thickness(8),
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(16, 16), target.DesiredSize);
        }
    }
}
