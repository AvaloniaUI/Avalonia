using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_NthChild
    {
        [Theory]
        [InlineData(2, 0, ":nth-child(2n)")]
        [InlineData(2, 1, ":nth-child(2n+1)")]
        [InlineData(1, 0, ":nth-child(1n)")]
        [InlineData(4, -1, ":nth-child(4n-1)")]
        [InlineData(0, 1, ":nth-child(1)")]
        [InlineData(0, -1, ":nth-child(-1)")]
        [InlineData(int.MaxValue, int.MinValue + 1, ":nth-child(2147483647n-2147483647)")]
        public void Not_Selector_Should_Have_Correct_String_Representation(int step, int offset, string expected)
        {
            var target = default(Selector).NthChild(step, offset);

            Assert.Equal(expected, target.ToString());
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel()
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

            var target = default(Selector).NthChild(2, 0);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.True(await target.Match(b2).Activator!.Take(1));
            Assert.False(await target.Match(b3).Activator!.Take(1));
            Assert.True(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Offset()
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

            var target = default(Selector).NthChild(2, 1);

            Assert.True(await target.Match(b1).Activator!.Take(1));
            Assert.False(await target.Match(b2).Activator!.Take(1));
            Assert.True(await target.Match(b3).Activator!.Take(1));
            Assert.False(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Negative_Offset()
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

            var target = default(Selector).NthChild(4, -1);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.False(await target.Match(b2).Activator!.Take(1));
            Assert.True(await target.Match(b3).Activator!.Take(1));
            Assert.False(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Singular_Step()
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

            var target = default(Selector).NthChild(1, 2);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.True(await target.Match(b2).Activator!.Take(1));
            Assert.True(await target.Match(b3).Activator!.Take(1));
            Assert.True(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Singular_Step_With_Negative_Offset()
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

            var target = default(Selector).NthChild(1, -1);

            Assert.True(await target.Match(b1).Activator!.Take(1));
            Assert.True(await target.Match(b2).Activator!.Take(1));
            Assert.True(await target.Match(b3).Activator!.Take(1));
            Assert.True(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Zero_Step_With_Offset()
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

            var target = default(Selector).NthChild(0, 2);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.True(await target.Match(b2).Activator!.Take(1));
            Assert.False(await target.Match(b3).Activator!.Take(1));
            Assert.False(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Doesnt_Match_Control_In_Panel_With_Zero_Step_With_Negative_Offset()
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

            var target = default(Selector).NthChild(0, -2);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.False(await target.Match(b2).Activator!.Take(1));
            Assert.False(await target.Match(b3).Activator!.Take(1));
            Assert.False(await target.Match(b4).Activator!.Take(1));
        }

        [Fact]
        public async Task Nth_Child_Match_Control_In_Panel_With_Previous_Selector()
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
            var target = previous.NthChild(2, 0);

            Assert.False(await target.Match(b1).Activator!.Take(1));
            Assert.True(await target.Match(b2).Activator!.Take(1));
            Assert.Null(target.Match(b3).Activator);
            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(b3).Result);
            Assert.Null(target.Match(b4).Activator);
            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(b4).Result);
        }

        [Fact]
        public void Nth_Child_Doesnt_Match_Control_Out_Of_Panel_Parent()
        {
            Border b1;
            var contentControl = new ContentControl();
            contentControl.Content = b1 = new Border();

            var target = default(Selector).NthChild(1, 0);

            Assert.Equal(SelectorMatch.NeverThisInstance, target.Match(b1));
        }


        [Theory] // http://nthmaster.com/
        [InlineData(+0, 8, false, false, false, false, false, false, false, true , false, false, false)]
        [InlineData(+1, 6, false, false, false, false, false, true , true , true , true , true , true )]
        [InlineData(-1, 9, true , true , true , true , true , true , true , true , true , false, false)]
        public async Task Nth_Child_Master_Com_Test_Sigle_Selector(
            int step, int offset, params bool[] items)
        {
            var panel = new StackPanel();
            panel.Children.AddRange(items.Select(_ => new Border()));

            var previous = default(Selector).OfType<Border>();
            var target = previous.NthChild(step, offset);

            var results = new bool[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                var border = panel.Children[index];
                results[index] = await target.Match(border).Activator!.Take(1);
            }

            Assert.Equal(items, results);
        }

        [Theory] // http://nthmaster.com/
        [InlineData(+1, 4, -1, 8, false, false, false, true , true , true , true , true , false, false, false)]
        [InlineData(+3, 1, +2, 0, false, false, false, true , false, false, false, false, false, true , false)]
        public async Task Nth_Child_Master_Com_Test_Double_Selector(
            int step1, int offset1, int step2, int offset2, params bool[] items)
        {
            var panel = new StackPanel();
            panel.Children.AddRange(items.Select(_ => new Border()));

            var previous = default(Selector).OfType<Border>();
            var middle = previous.NthChild(step1, offset1);
            var target = middle.NthChild(step2, offset2);

            var results = new bool[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                var border = panel.Children[index];
                results[index] = await target.Match(border).Activator!.Take(1);
            }

            Assert.Equal(items, results);
        }

        [Theory] // http://nthmaster.com/
        [InlineData(+1, 2, 2, 1, -1, 9, false, false, true , false, true , false, true , false, true , false, false)]
        public async Task Nth_Child_Master_Com_Test_Triple_Selector(
            int step1, int offset1, int step2, int offset2, int step3, int offset3, params bool[] items)
        {
            var panel = new StackPanel();
            panel.Children.AddRange(items.Select(_ => new Border()));

            var previous = default(Selector).OfType<Border>();
            var middle1 = previous.NthChild(step1, offset1);
            var middle2 = middle1.NthChild(step2, offset2);
            var target = middle2.NthChild(step3, offset3);

            var results = new bool[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                var border = panel.Children[index];
                results[index] = await target.Match(border).Activator!.Take(1);
            }

            Assert.Equal(items, results);
        }

        [Fact]
        public void Returns_Correct_TargetType()
        {
            var target = new NthChildSelector(default(Selector).OfType<Control1>(), 1, 0);

            Assert.Equal(typeof(Control1), target.TargetType);
        }

        public class Control1 : Control
        {
        }
    }
}
