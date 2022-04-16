using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_PropertyEquals
    {
        static SelectorTests_PropertyEquals()
        {
            //Ensure the attached properties are registered before run tests
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Grid).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Auth).TypeHandle);
        }

        class Auth
        {
            public readonly static AttachedProperty<string> NameProperty =
                AvaloniaProperty.RegisterAttached<Auth, AvaloniaObject, string>("Name");

            public static string GetName(AvaloniaObject avaloniaObject) =>
                avaloniaObject.GetValue(NameProperty);

            public static void SetName(AvaloniaObject avaloniaObject, string value) =>
                avaloniaObject.SetValue(NameProperty, value);
        }

        [Fact]
        public async Task PropertyEquals_Attached_Property_Matching_Value()
        {
            var target = new Markup.Parsers.SelectorParser((ns, type) =>
            {
                return (ns, type) switch
                {
                    ("", nameof(TextBlock)) => typeof(TextBlock),
                    ("", nameof(Grid)) => typeof(Grid),
                    _ => null
                };
            }).Parse("TextBlock[(Grid.Column)=1]");
            

            var control = new TextBlock();
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            Grid.SetColumn(control, 1);
            Assert.True(await activator.Take(1));
            Grid.SetColumn(control, 0);
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task PropertyEquals_Attached_Property_With_Namespace_Matching_Value()
        {
            var target = new Markup.Parsers.SelectorParser((ns, type) =>
            {
                return (ns, type) switch
                {
                    ("", nameof(TextBlock)) => typeof(TextBlock),
                    ("l", nameof(Auth)) => typeof(Auth),
                    _ => null
                };
            }).Parse("TextBlock[(l|Auth.Name)=Admin]");


            var control = new TextBlock();
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            Auth.SetName(control, "Admin");
            Assert.True(await activator.Take(1));
            Auth.SetName(control, null);
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task PropertyEquals_Matches_When_Property_Has_Matching_Value()
        {
            var control = new TextBlock();
            var target = default(Selector).PropertyEquals(TextBlock.TextProperty, "foo");
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            control.Text = "foo";
            Assert.True(await activator.Take(1));
            control.Text = null;
            Assert.False(await activator.Take(1));
        }

        [Theory]
        [InlineData("Bar", FooBar.Bar)]
        [InlineData("352", 352)]
        [InlineData("0.1", 0.1)]
        public async Task PropertyEquals_Matches_When_Property_Has_Matching_Value_And_Different_Type(string literal, object value)
        {
            var control = new TextBlock();
            var target = default(Selector).PropertyEquals(TextBlock.TagProperty, literal);
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            control.Tag = value;
            Assert.True(await activator.Take(1));
            control.Tag = null;
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void OfType_PropertyEquals_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new TextBlock();
            var target = default(Selector).OfType<Border>().PropertyEquals(TextBlock.TextProperty, "foo");

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }

        [Fact]
        public void PropertyEquals_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector)
                .OfType<TextBlock>()
                .PropertyEquals(TextBlock.TextProperty, "foo");

            Assert.Equal("TextBlock[Text=foo]", target.ToString());
        }

        private enum FooBar
        {
            Foo,
            Bar
        }
    }
}
