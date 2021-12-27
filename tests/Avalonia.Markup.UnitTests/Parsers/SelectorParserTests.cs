using System;
using Avalonia.Controls;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class SelectorParserTests
    {
        static SelectorParserTests()
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
        public void Parses_Boolean_Property_Selector()
        {
            var target = new SelectorParser((ns, type) => typeof(TextBlock));
            var result = target.Parse("TextBlock[IsPointerOver=True]");
        }

        [Fact]
        public void Parses_AttacchedProperty_Selector_With_Namespace()
        {
            var target = new SelectorParser((ns, type) =>
                {
                    return (ns, type) switch
                    {
                        ("", nameof(TextBlock)) => typeof(TextBlock),
                        ("l",nameof(Auth)) => typeof(Auth),
                        _ => null
                    };
                });
            var result = target.Parse("TextBlock[(l|Auth.Name)=Admin]");
        }

        [Fact]
        public void Parses_AttacchedProperty_Selector()
        {
            var target = new SelectorParser((ns, type) =>
            {
                return (ns, type) switch
                {
                    ("", nameof(TextBlock)) => typeof(TextBlock),
                    ("", nameof(Grid)) => typeof(Grid),
                    _ => null
                };
            });
            var result = target.Parse("TextBlock[(Grid.Column)=1]");
        }

        [Fact]
        public void Parses_Comma_Separated_Selectors()
        {
            var target = new SelectorParser((ns, type) => typeof(TextBlock));
            var result = target.Parse("TextBlock, TextBlock:foo");
        }

        [Fact]
        public void Throws_If_OfType_Type_Not_Found()
        {
            var target = new SelectorParser((ns, type) => null);
            Assert.Throws<InvalidOperationException>(() => target.Parse("NotFound"));
        }

        [Fact]
        public void Throws_If_Is_Type_Not_Found()
        {
            var target = new SelectorParser((ns, type) => null);
            Assert.Throws<InvalidOperationException>(() => target.Parse(":is(NotFound)"));
        }
    }
}
