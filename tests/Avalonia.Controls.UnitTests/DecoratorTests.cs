using System.Collections.Specialized;
using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
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

            Assert.Equal(new[] { child }, ((ILogical)decorator).LogicalChildren.ToList());
        }

        [Fact]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;
            decorator.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)decorator).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            decorator.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            decorator.Child = child;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            decorator.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            decorator.Child = child1;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) => called = true;

            decorator.Child = child2;

            Assert.True(called);
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

        public class UseLayoutRounding
        {
            [Fact]
            public void Measure_Rounds_Padding()
            {
                var target = new Decorator
                {
                    Padding = new Thickness(1),
                    Child = new Canvas
                    {
                        Width = 101,
                        Height = 101,
                    }
                };

                var root = CreatedRoot(1.5, target);

                root.LayoutManager.ExecuteInitialLayoutPass();

                // - 1 pixel padding is rounded up to 1.3333; for both sides it is 2.6666
                // - Size of 101 gets rounded up to 101.3333
                // - Desired size = 101.3333 + 2.6666 = 104
                Assert.Equal(new Size(104, 104), target.DesiredSize);
            }

            private static TestRoot CreatedRoot(
                double scaling,
                Control child,
                Size? constraint = null)
            {
                return new TestRoot
                {
                    LayoutScaling = scaling,
                    UseLayoutRounding = true,
                    Child = child,
                    ClientSize = constraint ?? new Size(1000, 1000),
                };
            }
        }
    }
}
