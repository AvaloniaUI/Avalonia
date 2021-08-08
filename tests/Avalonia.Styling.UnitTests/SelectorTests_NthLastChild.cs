using Avalonia.Controls;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_NthLastChild
    {
        [Theory]
        [InlineData(2, 0, ":nth-last-child(2n)")]
        [InlineData(2, 1, ":nth-last-child(2n+1)")]
        [InlineData(1, 0, ":nth-last-child(1n)")]
        [InlineData(4, -1, ":nth-last-child(4n-1)")]
        [InlineData(0, 1, ":nth-last-child(1)")]
        [InlineData(0, -1, ":nth-last-child(-1)")]
        [InlineData(int.MaxValue, int.MinValue + 1, ":nth-last-child(2147483647n-2147483647)")]
        public void Not_Selector_Should_Have_Correct_String_Representation(int step, int offset, string expected)
        {
            var target = default(Selector).NthLastChild(step, offset);

            Assert.Equal(expected, target.ToString());
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(2, 0);

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Offset()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(2, 1);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Negative_Offset()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(4, -1);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Singular_Step()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(1, 2);

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Singular_Step_With_Negative_Offset()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(1, -2);

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Zero_Step_With_Offset()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(0, 2);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Doesnt_Match_Control_In_Panel_With_Zero_Step_With_Negative_Offset()
        {
            Border b1, b2, b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Border(),
                b4 = new Border()
            });

            var target = default(Selector).NthLastChild(0, -2);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Match_Control_In_Panel_With_Previous_Selector()
        {
            Border b1, b2;
            Button b3, b4;
            var panel = new StackPanel();
            panel.Children.AddRange(new Control[]
            {
                b1 = new Border(),
                b2 = new Border(),
                b3 = new Button(),
                b4 = new Button()
            });

            var previous = default(Selector).OfType<Border>();
            var target = previous.NthLastChild(2, 0);

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(b1).Result);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b2).Result);
            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(b3).Result);
            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Doesnt_Match_Control_Out_Of_Panel_Parent()
        {
            Border b1;
            var contentControl = new ContentControl();
            contentControl.Content = b1 = new Border();

            var target = default(Selector).NthLastChild(1, 0);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(b1).Result);
        }

        [Fact]
        public void Returns_Correct_TargetType()
        {
            var target = new NthLastChildSelector(default(Selector).OfType<Control1>(), 1, 0);

            Assert.Equal(typeof(Control1), target.TargetType);
        }

        public class Control1 : Control
        {
        }
    }
}
